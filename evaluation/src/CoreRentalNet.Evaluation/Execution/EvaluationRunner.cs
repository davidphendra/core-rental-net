using CoreRentalNet.Evaluation.Cases;
using CoreRentalNet.Evaluation.Contracts;
using CoreRentalNet.Evaluation.Evidence;

namespace CoreRentalNet.Evaluation.Execution;

/// <summary>Runs each selected case with its own token and timeout, stopping on the first timeout.</summary>
public sealed class EvaluationRunner(
    IAgentClient agentClient,
    ITraceReader traceReader,
    ICallerTokenProvider tokenProvider,
    TimeSpan? caseTimeout = null)
{
    private readonly TimeSpan _caseTimeout = caseTimeout ?? TimeSpan.FromSeconds(15);

    public async Task<IReadOnlyList<RowRun>> RunAsync(
        EvaluationCaseSet caseSet,
        int repetitions,
        CancellationToken cancellationToken)
    {
        var runs = new List<RowRun>(caseSet.Cases.Count * repetitions);

        foreach (var testCase in caseSet.Cases)
        {
            for (var repetition = 1; repetition <= repetitions; repetition++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var token = await tokenProvider.GetAsync(cancellationToken).ConfigureAwait(false);
                var runId = $"eval-{testCase.CaseId}-r{repetition}";
                var request = RequestFactory.Create(testCase, runId);
                var events = new List<StreamEventDto>();
                var timedOut = false;

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
                }
                catch (OperationCanceledException) when (
                    timeout.IsCancellationRequested
                    && !cancellationToken.IsCancellationRequested)
                {
                    timedOut = true;
                }

                var trace = await traceReader.ReadAsync(runId, cancellationToken).ConfigureAwait(false);

                runs.Add(new RowRun(testCase, repetition, runId, events, trace)
                {
                    TimedOut = timedOut,
                });

                if (timedOut)
                {
                    throw new CaseTimeoutException(runs, _caseTimeout);
                }
            }
        }

        return runs;
    }
}
