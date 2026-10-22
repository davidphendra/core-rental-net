namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>One tool call's agent transport, guardrail and Host timing.</summary>
public sealed record ToolCallTiming(
    string Tool,
    double DurationMs,
    double GuardrailMs,
    double HostMs,
    IReadOnlyDictionary<string, double> ServerTiming);
