namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>One stage model call's duration, usage, and retriever phase.</summary>
public sealed record ModelCallTiming(
    string Stage,
    string? Phase,
    double DurationMs,
    int InputTokens,
    int OutputTokens,
    int? FinishReasonLength);
