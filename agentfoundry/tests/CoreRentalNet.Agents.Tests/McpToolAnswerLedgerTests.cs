using AwesomeAssertions;
using CoreRentalNet.Agents.Shared.Mcp;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>
/// The ledger of what the tools answered: it holds the tool's own bytes, it forgets them on demand, and it keeps
/// what it recorded when the caller moves on.
/// </summary>
/// <remarks>
/// The forgetting is the load-bearing part: a run makes one retrieval per attempt, and an accumulating ledger
/// would hand attempt two the products attempt one rejected — which is the loop the retry edge exists to break.
/// </remarks>
public sealed class McpToolAnswerLedgerTests
{
    [Fact]
    public void What_a_tool_answered_is_held_with_the_arguments_it_was_asked_with()
    {
        var ledger = new McpToolAnswerLedger();

        ledger.Record("search_catalogue", new Dictionary<string, object?> { ["category"] = "desk" }, "{\"a\":1}");

        ledger.RecordedAnswers.Should().ContainSingle();
        ledger.RecordedAnswers[0].ToolName.Should().Be("search_catalogue");
        ledger.RecordedAnswers[0].Arguments["category"].Should().Be("desk");
        ledger.RecordedAnswers[0].AnswerText.Should().Be("{\"a\":1}");
    }

    [Fact] // the function loop reuses its argument dictionary, so the ledger cannot hold the caller's
    public void The_arguments_are_copied_rather_than_borrowed()
    {
        var toolArguments = new Dictionary<string, object?> { ["category"] = "desk" };
        var ledger = new McpToolAnswerLedger();

        ledger.Record("search_catalogue", toolArguments, "{}");
        toolArguments["category"] = "chair";

        ledger.RecordedAnswers[0].Arguments["category"].Should().Be(
            "desk",
            "the arguments are what the tool was asked with, not what a later caller wrote into the same dictionary");
    }

    [Fact] // one attempt's searches are the only ones its reranker may consider
    public void Clearing_forgets_everything_recorded_so_far()
    {
        var ledger = new McpToolAnswerLedger();
        ledger.Record("search_catalogue", new Dictionary<string, object?>(), "{}");

        ledger.ClearRecordedAnswers();

        ledger.RecordedAnswers.Should().BeEmpty();
    }

    [Fact] // a turn may call several tools at once
    public void Recording_survives_being_called_from_several_threads()
    {
        var ledger = new McpToolAnswerLedger();

        Parallel.For(0, 200, position =>
            ledger.Record($"tool-{position}", new Dictionary<string, object?>(), "{}"));

        ledger.RecordedAnswers.Should().HaveCount(200);
    }
}
