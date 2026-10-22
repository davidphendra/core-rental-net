namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>One stage model call's duration and usage.</summary>
public sealed record ModelCallTiming(
    string Stage,
    double DurationMs,
    int InputTokens,
    int OutputTokens,
    int? FinishReasonLength);
