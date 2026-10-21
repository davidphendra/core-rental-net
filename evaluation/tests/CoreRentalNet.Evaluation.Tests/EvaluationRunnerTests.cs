using System.Runtime.CompilerServices;
using AwesomeAssertions;
using CoreRentalNet.Evaluation.Cases;
using CoreRentalNet.Evaluation.Evidence;
using CoreRentalNet.Evaluation.Execution;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests;

public sealed class EvaluationRunnerTests
{
    [Fact]
    public async Task A_case_that_exceeds_the_budget_stops_the_run_and_is_marked_timed_out()
    {
        var caseSet = new EvaluationCaseSet([TestScriptManifest.Cases[0]]);
        var runner = new EvaluationRunner(
            new SlowAgentClient(),
            new EmptyTraceReader(),
            new FixedTokenProvider(),
            TimeSpan.FromMilliseconds(20));

        var act = () => runner.RunAsync(caseSet, repetitions: 1, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<CaseTimeoutException>();
        exception.Which.Runs.Should().ContainSingle();
        exception.Which.Runs[0].TimedOut.Should().BeTrue();
    }

    private sealed class SlowAgentClient : IAgentClient
    {
        public async IAsyncEnumerable<string> RunAsync(
            string requestJson,
            string callerToken,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
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
