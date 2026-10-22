namespace CoreRentalNet.Agents.Shared.Evaluation;

/// <summary>One completed activity from the evaluation run, retained as evidence.</summary>
internal sealed record EvaluationSpanRecord(
    string RunId,
    string TraceId,
    string SpanId,
    string? ParentSpanId,
    string Name,
    string Kind,
    DateTimeOffset StartedAt,
    double DurationMs,
    IReadOnlyDictionary<string, string?> Tags);
