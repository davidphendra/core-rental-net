using System.Runtime.CompilerServices;
using AwesomeAssertions;
using CoreRentalNet.Evaluation.Cases.Model;
using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;
using CoreRentalNet.Evaluation.Evidence.Trace;
using CoreRentalNet.Evaluation.Evidence.Projection;
using CoreRentalNet.Evaluation.Evidence.Context;
using CoreRentalNet.Evaluation.Execution.Transport;
using CoreRentalNet.Evaluation.Execution.Auth;
using CoreRentalNet.Evaluation.Execution.Tools;
using CoreRentalNet.Evaluation.Execution.Running;
using Xunit;

using CoreRentalNet.Evaluation.Tests.Fixtures;

namespace CoreRentalNet.Evaluation.Tests.Execution;

public sealed class EvaluationRunnerTests
{
    [Fact]
    public async Task A_case_that_exceeds_the_budget_is_dropped_and_the_next_case_runs()
    {
        var caseSet = new EvaluationCaseSet([TestScriptManifest.Cases[0], TestScriptManifest.Cases[1]]);
        var runner = new EvaluationRunner(
            new SlowThenFastAgentClient(),
            new EmptyTraceReader(),
            new FixedTokenProvider(),
            TimeSpan.FromMilliseconds(20));

        var runs = await runner.RunAsync(caseSet, repetitions: 1, CancellationToken.None);

        runs.Should().HaveCount(2);
        runs[0].TimedOut.Should().BeTrue();
        runs[1].TimedOut.Should().BeFalse();
    }

    [Fact]
    public async Task Cases_are_limited_to_the_requested_concurrency()
    {
        var caseSet = new EvaluationCaseSet([.. TestScriptManifest.Cases.Take(5)]);
        var agentClient = new ConcurrencyRecordingAgentClient();
        var runner = new EvaluationRunner(
            agentClient,
            new EmptyTraceReader(),
            new FixedTokenProvider(),
            TimeSpan.FromSeconds(2),
            maximumConcurrentCases: 3);

        await runner.RunAsync(caseSet, repetitions: 1, CancellationToken.None);

        agentClient.MaximumConcurrentCalls.Should().Be(3);
    }

    [Fact]
    public async Task Cases_are_started_concurrently()
    {
        var caseSet = new EvaluationCaseSet([TestScriptManifest.Cases[0], TestScriptManifest.Cases[1]]);
        var agentClient = new ConcurrencyRecordingAgentClient();
        var runner = new EvaluationRunner(
            agentClient,
            new EmptyTraceReader(),
            new FixedTokenProvider(),
            TimeSpan.FromSeconds(2));

        await runner.RunAsync(caseSet, repetitions: 1, CancellationToken.None);

        agentClient.MaximumConcurrentCalls.Should().Be(2);
    }

    private sealed class ConcurrencyRecordingAgentClient : IAgentClient
    {
        private int _concurrentCalls;
        private int _maximumConcurrentCalls;

        public int MaximumConcurrentCalls => _maximumConcurrentCalls;

        private void UpdateMaximum(int concurrent)
        {
            var maximum = _maximumConcurrentCalls;
            while (concurrent > maximum)
            {
                var original = Interlocked.CompareExchange(
                    ref _maximumConcurrentCalls,
                    concurrent,
                    maximum);

                if (original == maximum)
                {
                    return;
                }

                maximum = original;
            }
        }

        public async IAsyncEnumerable<string> RunAsync(
            string requestJson,
            string callerToken,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var concurrent = Interlocked.Increment(ref _concurrentCalls);
            UpdateMaximum(concurrent);

            await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
            Interlocked.Decrement(ref _concurrentCalls);
            yield break;
        }
    }

    private sealed class SlowThenFastAgentClient : IAgentClient
    {
        public async IAsyncEnumerable<string> RunAsync(
            string requestJson,
            string callerToken,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (requestJson.Contains("eval-TS-001-r1", StringComparison.Ordinal))
            {
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }

            yield break;
        }
    }

    private sealed class EmptyTraceReader : ITraceReader
    {
        public Task<RunTrace> ReadAsync(string runId, CancellationToken cancellationToken)
            => Task.FromResult(new RunTrace(runId, [], []));
    }

    private sealed class FixedTokenProvider : ICallerTokenProvider
    {
        public Task<CallerToken> GetAsync(CancellationToken cancellationToken)
            => Task.FromResult(new CallerToken(
                "token",
                DateTimeOffset.UtcNow.AddHours(1),
                null,
                [],
                []));
    }
}
