using System.Diagnostics;
using CoreRentalNet.Evaluation.Cases.Model;
using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;
using CoreRentalNet.Evaluation.Contracts.Suggestion;
using CoreRentalNet.Evaluation.Contracts.Catalogue;
using CoreRentalNet.Evaluation.Contracts.Reranker;
using CoreRentalNet.Evaluation.Contracts.Trace;
using CoreRentalNet.Evaluation.Evidence.Trace;
using CoreRentalNet.Evaluation.Evidence.Projection;
using CoreRentalNet.Evaluation.Evidence.Context;

using CoreRentalNet.Evaluation.Execution.Transport;
using CoreRentalNet.Evaluation.Execution.Auth;
using CoreRentalNet.Evaluation.Execution.Tools;
namespace CoreRentalNet.Evaluation.Execution.Running;

/// <summary>Runs every selected case concurrently with its own token and timeout; a timed-out case is dropped.</summary>
public sealed class EvaluationRunner(
    IAgentClient agentClient,
    ITraceReader traceReader,
    ICallerTokenProvider tokenProvider,
    TimeSpan? caseTimeout = null,
    int maximumConcurrentCases = int.MaxValue)
{
    private readonly TimeSpan _caseTimeout = caseTimeout ?? TimeSpan.FromSeconds(15);

    private readonly int _maximumConcurrentCases = maximumConcurrentCases > 0
        ? maximumConcurrentCases
        : throw new ArgumentOutOfRangeException(
            nameof(maximumConcurrentCases),
            maximumConcurrentCases,
            "The concurrency limit must be greater than zero.");

    public async Task<IReadOnlyList<RowRun>> RunAsync(
        EvaluationCaseSet caseSet,
        int repetitions,
        CancellationToken cancellationToken)
    {
        var order = caseSet.Cases
            .Select((testCase, index) => (testCase, index))
            .ToDictionary(pair => pair.testCase, pair => pair.index);

        using var concurrencyGate = new SemaphoreSlim(
            _maximumConcurrentCases,
            _maximumConcurrentCases);

        var tasks = caseSet.Cases
            .SelectMany(testCase => Enumerable.Range(1, repetitions)
                .Select(repetition => RunWithConcurrencyGateAsync(
                    concurrencyGate,
                    testCase,
                    repetition,
                    cancellationToken)))
            .ToArray();

        var runs = await Task.WhenAll(tasks).ConfigureAwait(false);

        return [.. runs
            .OrderBy(run => order[run.Case])
            .ThenBy(run => run.Repetition)];
    }

    private async Task<RowRun> RunWithConcurrencyGateAsync(
        SemaphoreSlim concurrencyGate,
        EvaluationCase testCase,
        int repetition,
        CancellationToken cancellationToken)
    {
        await concurrencyGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            return await RunOneAsync(testCase, repetition, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            concurrencyGate.Release();
        }
    }

    private async Task<RowRun> RunOneAsync(
        EvaluationCase testCase,
        int repetition,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var token = await tokenProvider.GetAsync(cancellationToken).ConfigureAwait(false);
        var runId = $"eval-{testCase.CaseId}-r{repetition}";
        var request = RequestFactory.Create(testCase, runId);
        var events = new List<StreamEventDto>();
        var timedOut = false;
        var completed = false;
        var stopwatch = Stopwatch.StartNew();

        using var timeout = new CancellationTokenSource(_caseTimeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeout.Token);

        try
        {
            await foreach (var json in agentClient
                .RunAsync(request, token.Value, linked.Token)
                .WithCancellation(linked.Token))
            {
                events.Add(StreamEventReader.Read(json));
            }

            completed = true;
        }
        catch (OperationCanceledException) when (
            timeout.IsCancellationRequested
            && !cancellationToken.IsCancellationRequested)
        {
            timedOut = true;
        }

        if (timedOut)
        {
            // The activity listener writes the stopped span as the node unwinds. Give it a moment to
            // flush before reading the trace, or the timing report loses the only evidence available.
            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken).ConfigureAwait(false);
        }

        var trace = await traceReader.ReadAsync(runId, cancellationToken).ConfigureAwait(false);

        return new RowRun(testCase, repetition, runId, events, trace)
        {
            TimedOut = timedOut && !completed,
            Completed = completed,
            WallMs = stopwatch.Elapsed.TotalMilliseconds,
        };
    }
}
