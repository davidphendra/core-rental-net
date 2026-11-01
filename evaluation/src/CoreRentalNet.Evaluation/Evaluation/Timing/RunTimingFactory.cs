using CoreRentalNet.Evaluation.Evidence.Trace;
using CoreRentalNet.Evaluation.Evidence.Projection;
using CoreRentalNet.Evaluation.Evidence.Context;

using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;
namespace CoreRentalNet.Evaluation.Evaluation.Timing;

/// <summary>Builds a timing tree from the agent's activity spans.</summary>
public static class RunTimingFactory
{
    private const string NodeKindTag = "node.kind";
    private const string StageTag = "stage";
    private const string StageAgentTag = "stage.agent.name";
    private const string PhaseTag = "chat.call.phase";
    private const string ToolNameTag = "tool.name";
    private const string GuardPrefix = "guardrail.";
    private const string ModelName = "stage-model-call";
    private const string ToolName = "tool.invoke";
    private const string McpName = "mcp.http";
    private const string ServerTimingTag = "mcp.server_timing";

    public static RunTiming Create(RunTrace trace, double wallMs = 0)
    {
        ArgumentNullException.ThrowIfNull(trace);

        var spans = trace.Spans;
        var nodes = spans
            .Where(span => span.Tags.ContainsKey(NodeKindTag))
            .ToArray();
        var bySpan = spans.ToDictionary(span => span.SpanId, StringComparer.Ordinal);

        var stages = nodes
            .Select(node => Stage(node, spans, bySpan))
            .ToArray();

        var modelCalls = spans
            .Where(span => string.Equals(span.Name, ModelName, StringComparison.Ordinal))
            .Select(Model)
            .ToArray();

        var toolCalls = spans
            .Where(span => string.Equals(span.Name, ToolName, StringComparison.Ordinal))
            .Select(tool => Tool(tool, spans, bySpan))
            .ToArray();

        var totalMs = stages.Sum(stage => stage.TotalMs);

        return new RunTiming(
            TotalMs: totalMs,
            Stages: stages,
            ModelCalls: modelCalls,
            ToolCalls: toolCalls)
        {
            WallMs = wallMs > 0 ? wallMs : totalMs,
        };
    }

    private static StageTiming Stage(
        TraceSpan node,
        IReadOnlyList<TraceSpan> spans,
        IReadOnlyDictionary<string, TraceSpan> bySpan)
    {
        var descendants = spans.Where(span => IsUnder(span, node, bySpan)).ToArray();
        var toolMs = descendants
            .Where(span => string.Equals(span.Name, ToolName, StringComparison.Ordinal))
            .Sum(span => span.DurationMs);
        var modelCalls = descendants
            .Where(span => string.Equals(span.Name, ModelName, StringComparison.Ordinal))
            .ToArray();
        var modelMs = modelCalls.Sum(span => span.DurationMs);
        var toolPlanningModelMs = modelCalls
            .Where(span => string.Equals(
                span.Tags.GetValueOrDefault(PhaseTag),
                "tool-planning",
                StringComparison.Ordinal))
            .Sum(span => span.DurationMs);
        var finalStructuredModelMs = modelCalls
            .Where(span => string.Equals(
                span.Tags.GetValueOrDefault(PhaseTag),
                "final-structured",
                StringComparison.Ordinal))
            .Sum(span => span.DurationMs);

        // A model-backed node whose model span never stopped, because the case timed out, still has a measured
        // node duration. Attribute the non-tool time to the model rather than dropping it into unattributed.
        if (modelMs == 0
            && string.Equals(node.Tags.GetValueOrDefault(NodeKindTag), "model", StringComparison.Ordinal))
        {
            modelMs = Math.Max(0, node.DurationMs - toolMs);
        }
        var guardrailMs = descendants
            .Where(span => span.Name.StartsWith(GuardPrefix, StringComparison.Ordinal))
            .Sum(span => span.DurationMs);
        var hostMs = descendants
            .Where(span => string.Equals(span.Name, McpName, StringComparison.Ordinal))
            .Sum(HostDuration);

        return new StageTiming(
            Stage: node.Tags.GetValueOrDefault(StageTag) ?? node.Name,
            TotalMs: node.DurationMs,
            ModelMs: modelMs,
            ToolMs: toolMs,
            GuardrailMs: guardrailMs,
            HostMs: hostMs,
            UnattributedMs: Math.Max(0, node.DurationMs - modelMs - toolMs))
        {
            ToolPlanningModelMs = toolPlanningModelMs,
            FinalStructuredModelMs = finalStructuredModelMs,
            ModelCallCount = modelCalls.Length,
            ToolCallCount = descendants.Count(span =>
                string.Equals(span.Name, ToolName, StringComparison.Ordinal)),
        };
    }

    private static ModelCallTiming Model(TraceSpan span)
        => new(
            Stage: span.Tags.GetValueOrDefault(StageAgentTag) ?? "(unnamed stage)",
            Phase: span.Tags.GetValueOrDefault(PhaseTag),
            DurationMs: span.DurationMs,
            InputTokens: Int(span, "gen_ai.usage.input_tokens"),
            OutputTokens: Int(span, "gen_ai.usage.output_tokens"),
            FinishReasonLength: null);

    private static ToolCallTiming Tool(
        TraceSpan tool,
        IReadOnlyList<TraceSpan> spans,
        IReadOnlyDictionary<string, TraceSpan> bySpan)
    {
        var descendants = spans.Where(span => IsUnder(span, tool, bySpan)).ToArray();
        var guardrailMs = descendants
            .Where(span => span.Name.StartsWith(GuardPrefix, StringComparison.Ordinal))
            .Sum(span => span.DurationMs);
        var mcp = descendants
            .Where(span => string.Equals(span.Name, McpName, StringComparison.Ordinal))
            .ToArray();
        var serverTiming = mcp
            .Select(span => ServerTiming.Parse(span.Tags.GetValueOrDefault(ServerTimingTag)))
            .SelectMany(values => values)
            .GroupBy(pair => pair.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Sum(pair => pair.Value), StringComparer.Ordinal);

        return new ToolCallTiming(
            Tool: tool.Tags.GetValueOrDefault(ToolNameTag) ?? "(unnamed tool)",
            DurationMs: tool.DurationMs,
            GuardrailMs: guardrailMs,
            HostMs: serverTiming.GetValueOrDefault("mcp"),
            ServerTiming: serverTiming);
    }

    private static bool IsUnder(
        TraceSpan span,
        TraceSpan ancestor,
        IReadOnlyDictionary<string, TraceSpan> bySpan)
    {
        var current = span;

        while (current.ParentSpanId is { } parentId
               && bySpan.TryGetValue(parentId, out var parent))
        {
            if (string.Equals(parentId, ancestor.SpanId, StringComparison.Ordinal))
            {
                return true;
            }

            current = parent;
        }

        return false;
    }

    private static double HostDuration(TraceSpan span)
        => ServerTiming.Parse(span.Tags.GetValueOrDefault(ServerTimingTag))
            .GetValueOrDefault("mcp");

    private static int Int(TraceSpan span, string name)
        => int.TryParse(span.Tags.GetValueOrDefault(name), out var value) ? value : 0;
}
