namespace CoreRentalNet.Evaluation.Evidence;

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
