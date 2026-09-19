using System.Text.Json;
using AwesomeAssertions;
using WorkspaceSuggestions.Agents;
using WorkspaceSuggestions.Usage;
using WorkspaceSuggestions.Workflows;
using Xunit;

namespace WorkspaceSuggestions.Tests;

/// <summary>
/// The run's cost attached to the answer, on both paths, and the counting scope left closed afterwards.
/// </summary>
public sealed class UsageReportingAgentTests
{
    private const string Spec =
        """
        { "status": "spec", "reason": null, "ceilingMonthly": null,
          "slots": [ { "slot": "Desk", "quantity": 1, "purpose": "a wide, stable surface" } ],
          "constraints": [] }
        """;

    private const string Result =
        """
        { "status": "suggested", "reason": null,
          "options": [ { "lines": [ { "slot": "Desk", "sku": "DSKB08XN4JDR", "quantity": 1, "why": "a stable surface" } ],
                         "rationale": "A calm, focused setup." } ] }
        """;

    [Fact]
    public async Task The_streamed_answer_ends_with_what_the_run_cost()
    {
        var updates = new List<string>();
        var agent = Build();

        await foreach (var update in agent.RunStreamingAsync("{}"))
        {
            updates.Add(update.Text ?? string.Empty);
        }

        var text = string.Concat(updates);

        // The answer is untouched: the specification, then the result, still in that order.
        text.IndexOf("\"spec\"", StringComparison.Ordinal)
            .Should().BeLessThan(text.IndexOf("\"suggested\"", StringComparison.Ordinal));

        // And the cost follows it as its own object, last, because it is only known once the run has finished.
        // Read as JSON rather than asserted by its bytes: the default encoder escapes '+' as \u002B, which
        // decodes back correctly, so the report is checked for what it says and not for how it is spelled.
        using var report = JsonDocument.Parse(updates[^1]);
        var usage = report.RootElement.GetProperty("runUsage");

        usage.GetProperty("modelCalls").GetInt32().Should().Be(2, "the workflow made two model calls and code counted them");
        usage.GetProperty("model").GetString().Should().Be("gpt-4.1-mini");
        usage.GetProperty("promptVersion").GetString().Should().Be("rephraser.v1+suggestor.v1");
    }

    [Fact]
    public async Task The_unstreamed_answer_carries_it_too_so_one_rule_covers_both_paths()
    {
        var text = (await Build().RunAsync("{}")).Text!;

        text.Should().Contain("\"suggested\"");
        text.Should().Contain("\"runUsage\"");
        text.Should().Contain("\"modelCalls\":2");
    }

    [Fact]
    public async Task What_the_wrapper_writes_satisfies_the_run_usage_schema()
    {
        // The report is written by hand-written code, so it is the only part of the answer that is not pinned
        // by the model's own structured output. This is what pins it.
        var updates = new List<string>();

        await foreach (var update in Build().RunStreamingAsync("{}"))
        {
            updates.Add(update.Text ?? string.Empty);
        }

        Schemas.EvaluateRaw("run-usage.schema.json", updates[^1]).IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task The_counting_scope_is_closed_when_the_run_ends()
    {
        // A scope left open would bill the next customer's calls to this run.
        await foreach (var _ in Build().RunStreamingAsync("{}"))
        {
        }

        RunUsageScope.Current.Should().BeNull();
    }

    [Fact]
    public async Task A_run_that_fails_still_closes_its_scope()
    {
        // Whether the failure surfaces as an exception or as an error the framework returns, what must not
        // happen is a scope left open - it would bill the next customer's calls to this run.
        var client = new UsageRecordingChatClient(new ThrowingChatClient());

        var workflow = new WorkspaceSuggestionWorkflow(
                AgentFactory.Build(AgentRoster.Rephraser, client),
                AgentFactory.Build(AgentRoster.Suggestor, client))
            .AsAIAgent();

        var agent = new UsageReportingAgent(workflow, "gpt-4.1-mini", "rephraser.v1+suggestor.v1");

        try
        {
            await foreach (var _ in agent.RunStreamingAsync("{}"))
            {
            }
        }
        catch (Exception)
        {
            // Expected: the model client threw.
        }

        RunUsageScope.Current.Should().BeNull("a failed run must not wedge the next one's counting");
    }

    /// <summary>The real shape: the shared client counts, and the workflow is what is wrapped.</summary>
    private static UsageReportingAgent Build()
    {
        var client = new UsageRecordingChatClient(new ScriptedChatClient(Spec, Result));

        var workflow = new WorkspaceSuggestionWorkflow(
                AgentFactory.Build(AgentRoster.Rephraser, client),
                AgentFactory.Build(AgentRoster.Suggestor, client))
            .AsAIAgent();

        return new UsageReportingAgent(workflow, "gpt-4.1-mini", "rephraser.v1+suggestor.v1");
    }
}
