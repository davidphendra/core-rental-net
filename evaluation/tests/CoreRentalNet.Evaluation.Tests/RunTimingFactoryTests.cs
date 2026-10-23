using AwesomeAssertions;
using CoreRentalNet.Evaluation.Evaluation;
using CoreRentalNet.Evaluation.Evidence;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests;

/// <summary>The timing tree separates tool execution from the two retriever model phases.</summary>
public sealed class RunTimingFactoryTests
{
    [Fact]
    public void A_retriever_node_exposes_tool_time_and_both_model_phases()
    {
        var node = Span("node", "catalogue-product-retriever", null, "model",
            ("stage", "catalogue-product-retriever"), ("node.kind", "model"));
        var tool = Span("tool", "tool.invoke", "node", "Client",
            ("tool.name", "search_catalogue"));
        var planning = Span("planning", "stage-model-call", "node", "Client",
            ("stage.agent.name", "catalogue-product-retriever"),
            ("chat.call.phase", "tool-planning"));
        var final = Span("final", "stage-model-call", "node", "Client",
            ("stage.agent.name", "catalogue-product-retriever"),
            ("chat.call.phase", "final-structured"));

        var timing = RunTimingFactory.Create(
            new RunTrace("run", [], []) { Spans = [node, tool, planning, final] },
            wallMs: 5_000);

        var stage = timing.Stages.Single();
        stage.ToolMs.Should().Be(150);
        stage.ToolPlanningModelMs.Should().Be(1_000);
        stage.FinalStructuredModelMs.Should().Be(2_000);
        stage.ModelCallCount.Should().Be(2);
        stage.ToolCallCount.Should().Be(1);
        timing.WallMs.Should().Be(5_000);
        timing.UnaccountedMs.Should().Be(5_000 - node.DurationMs);
    }

    private static TraceSpan Span(
        string id,
        string name,
        string? parentId,
        string kind,
        params (string Key, string Value)[] tags)
        => new(
            RunId: "run",
            TraceId: "trace",
            SpanId: id,
            ParentSpanId: parentId,
            Name: name,
            Kind: kind,
            StartedAt: DateTimeOffset.UtcNow,
            DurationMs: name switch
            {
                "tool.invoke" => 150,
                "stage-model-call" when id == "planning" => 1_000,
                "stage-model-call" => 2_000,
                _ => 3_000,
            },
            Tags: tags.ToDictionary(tag => tag.Key, tag => (string?)tag.Value, StringComparer.Ordinal));
}
