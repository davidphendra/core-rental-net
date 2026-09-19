using AwesomeAssertions;
using Microsoft.Extensions.AI;
using WorkspaceSuggestions.Usage;
using Xunit;

namespace WorkspaceSuggestions.Tests;

/// <summary>
/// The run's cost, counted by the code that made the calls rather than guessed by the model that could not
/// observe them.
/// </summary>
public sealed class UsageRecorderTests
{
    [Fact]
    public void A_call_is_counted_even_when_the_client_reports_nothing()
    {
        // The call count is always a measurement; the token counts are only as good as what the client says.
        var recorder = new UsageRecorder();

        recorder.Called();
        recorder.Called();

        recorder.ModelCalls.Should().Be(2);
        recorder.InputTokens.Should().Be(0);
        recorder.OutputTokens.Should().Be(0);
    }

    [Fact]
    public void What_each_call_reports_is_summed_across_the_run()
    {
        var recorder = new UsageRecorder();

        recorder.Called();
        recorder.Record(new UsageDetails { InputTokenCount = 86_000, OutputTokenCount = 400 });
        recorder.Called();
        recorder.Record(new UsageDetails { InputTokenCount = 86_400, OutputTokenCount = 350 });

        recorder.ModelCalls.Should().Be(2);
        recorder.InputTokens.Should().Be(172_400);
        recorder.OutputTokens.Should().Be(750);
    }

    [Fact]
    public void A_report_that_is_absent_changes_nothing()
    {
        var recorder = new UsageRecorder();

        recorder.Called();
        recorder.Record(null);

        recorder.ModelCalls.Should().Be(1);
        recorder.InputTokens.Should().Be(0);
    }

    [Fact]
    public void A_report_carrying_only_one_half_counts_that_half()
    {
        var recorder = new UsageRecorder();

        recorder.Record(new UsageDetails { InputTokenCount = 1_000 });

        recorder.InputTokens.Should().Be(1_000);
        recorder.OutputTokens.Should().Be(0);
    }

    [Fact]
    public void The_snapshot_carries_the_counted_numbers_and_the_two_configured_facts()
    {
        var recorder = new UsageRecorder();

        recorder.Called();
        recorder.Record(new UsageDetails { InputTokenCount = 10, OutputTokenCount = 2 });

        var usage = recorder.Snapshot("gpt-4.1-mini", "suggestor.v1");

        usage.ModelCalls.Should().Be(1);
        usage.InputTokens.Should().Be(10);
        usage.OutputTokens.Should().Be(2);
        usage.Model.Should().Be("gpt-4.1-mini");
        usage.PromptVersion.Should().Be("suggestor.v1");
    }

    [Fact]
    public void A_count_beyond_the_contract_s_ceiling_saturates_rather_than_wrapping()
    {
        // The contract carries integers. A bug that accumulated across runs - the very failure this whole
        // design exists to prevent - must read as absurd rather than as a small negative number.
        var recorder = new UsageRecorder();

        recorder.Record(new UsageDetails { InputTokenCount = long.MaxValue / 2 });
        recorder.Record(new UsageDetails { InputTokenCount = long.MaxValue / 2 });

        recorder.Snapshot("m", "v").InputTokens.Should().Be(int.MaxValue);
    }

    [Fact]
    public async Task Counting_survives_concurrent_calls()
    {
        var recorder = new UsageRecorder();

        await Task.WhenAll(Enumerable.Range(0, 100).Select(_ => Task.Run(() =>
        {
            for (var i = 0; i < 100; i++)
            {
                recorder.Called();
                recorder.Record(new UsageDetails { InputTokenCount = 1, OutputTokenCount = 1 });
            }
        })));

        recorder.ModelCalls.Should().Be(10_000);
        recorder.InputTokens.Should().Be(10_000);
        recorder.OutputTokens.Should().Be(10_000);
    }
}
