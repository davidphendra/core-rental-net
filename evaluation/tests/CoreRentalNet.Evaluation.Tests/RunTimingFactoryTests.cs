using AwesomeAssertions;
using CoreRentalNet.Evaluation.Evaluation;
using CoreRentalNet.Evaluation.Evidence;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests;

public sealed class RunTimingFactoryTests
{
    [Fact]
    public void Node_model_tool_and_guardrail_time_are_separated()
    {
        var node = Span("node", "workspace-request-verifier", 100, null, new Dictionary<string, string?>
        {
            ["node.kind"] = "model",
            ["stage"] = "workspace-request-verifier",
        });
        var model = Span("model", "stage-model-call", 70, "node", new Dictionary<string, string?>
        {
            ["stage.agent.name"] = "workspace-request-verifier",
        });
        var tool = Span("tool", "tool.invoke", 25, "node", new Dictionary<string, string?> { ["tool.name"] = "search_catalogue" });
        var guard = Span("guard", "guardrail.before", 2, "tool", new Dictionary<string, string?> { ["guard"] = "ToolAllowListGuard" });
        var mcp = Span("mcp", "mcp.http", 20, "tool", new Dictionary<string, string?>
        {
            ["mcp.server_timing"] = "mcp;dur=18, embedding;dur=8",
        });

        var timing = RunTimingFactory.Create(Trace(node, model, tool, guard, mcp));

        timing.Stages.Should().ContainSingle();
        timing.Stages[0].TotalMs.Should().Be(100);
        timing.Stages[0].ModelMs.Should().Be(70);
        timing.Stages[0].ToolMs.Should().Be(25);
        timing.Stages[0].GuardrailMs.Should().Be(2);
        timing.Stages[0].HostMs.Should().Be(18);
        timing.Stages[0].UnattributedMs.Should().Be(5);
        timing.ToolCalls.Single().ServerTiming["embedding"].Should().Be(8);
    }

    private static RunTrace Trace(params TraceSpan[] spans)
        => new("run", [], []) { Spans = spans };

    private static TraceSpan Span(
        string id,
        string name,
        double durationMs,
        string? parent,
        IReadOnlyDictionary<string, string?> tags)
        => new(
            "run",
            "trace",
            id,
            parent,
            name,
            "Internal",
            DateTimeOffset.UnixEpoch,
            durationMs,
            tags);
}
