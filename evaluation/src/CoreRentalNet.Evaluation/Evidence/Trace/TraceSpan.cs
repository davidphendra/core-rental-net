using CoreRentalNet.Evaluation.Evidence.Projection;
using CoreRentalNet.Evaluation.Evidence.Context;

namespace CoreRentalNet.Evaluation.Evidence.Trace;

/// <summary>One completed activity from the agent, with the tags the timing report needs.</summary>
public sealed record TraceSpan(
    string RunId,
    string TraceId,
    string SpanId,
    string? ParentSpanId,
    string Name,
    string Kind,
    DateTimeOffset StartedAt,
    double DurationMs,
    IReadOnlyDictionary<string, string?> Tags);
