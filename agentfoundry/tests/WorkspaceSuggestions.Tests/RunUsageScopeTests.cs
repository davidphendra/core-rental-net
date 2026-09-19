using AwesomeAssertions;
using WorkspaceSuggestions.Usage;
using Xunit;

namespace WorkspaceSuggestions.Tests;

/// <summary>
/// Which run the counting belongs to. The chat client is shared by every request, so without this one
/// customer's tokens would be recorded against the next customer's run.
/// </summary>
public sealed class RunUsageScopeTests
{
    [Fact]
    public void Nothing_is_being_counted_until_a_scope_begins()
    {
        RunUsageScope.Current.Should().BeNull();
    }

    [Fact]
    public void A_scope_counts_into_its_own_recorder_and_stops_when_it_ends()
    {
        using (var scope = RunUsageScope.Begin())
        {
            RunUsageScope.Current.Should().BeSameAs(scope.Recorder);

            scope.Recorder.Called();
        }

        RunUsageScope.Current.Should().BeNull();
    }

    [Fact]
    public void A_run_inside_a_run_leaves_the_outer_record_exactly_as_it_found_it()
    {
        // A repair loop, or a future evaluation pass, must not be billed to the run that contains it.
        using var outer = RunUsageScope.Begin();
        outer.Recorder.Record(new Microsoft.Extensions.AI.UsageDetails { InputTokenCount = 100 });

        using (var inner = RunUsageScope.Begin())
        {
            RunUsageScope.Current.Should().BeSameAs(inner.Recorder);

            inner.Recorder.Record(new Microsoft.Extensions.AI.UsageDetails { InputTokenCount = 5 });
        }

        RunUsageScope.Current.Should().BeSameAs(outer.Recorder);
        outer.Recorder.InputTokens.Should().Be(100);
    }

    [Fact]
    public async Task The_scope_follows_the_async_flow_of_one_run()
    {
        using var scope = RunUsageScope.Begin();

        async Task<UsageRecorder?> Counted() => await Task.Run(() =>
        {
            RunUsageScope.Current?.Called();

            return RunUsageScope.Current;
        });

        (await Counted()).Should().BeSameAs(scope.Recorder);
        scope.Recorder.ModelCalls.Should().Be(1);
    }

    [Fact]
    public async Task Two_runs_in_flight_do_not_count_into_each_other()
    {
        // The reason this is ambient rather than a field on the shared client. Awaiting between opening the
        // scope and recording leaves both runs open at once, which is the case that would mix them up.
        async Task<long> RunAsync(int tokens)
        {
            using var scope = RunUsageScope.Begin();

            await Task.Yield();
            scope.Recorder.Record(new Microsoft.Extensions.AI.UsageDetails { InputTokenCount = tokens });
            await Task.Yield();

            return scope.Recorder.InputTokens;
        }

        var results = await Task.WhenAll(RunAsync(tokens: 10), RunAsync(tokens: 999));

        results.Should().Equal(10, 999);
    }

    [Fact]
    public void Ending_a_scope_twice_is_safe()
    {
        var scope = RunUsageScope.Begin();

        scope.Dispose();
        scope.Dispose();

        RunUsageScope.Current.Should().BeNull();
    }
}
