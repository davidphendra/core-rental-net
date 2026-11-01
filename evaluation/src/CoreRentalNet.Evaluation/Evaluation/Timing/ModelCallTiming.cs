using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;

namespace CoreRentalNet.Evaluation.Evaluation.Timing;

/// <summary>One stage model call's duration, usage, and retriever phase.</summary>
public sealed record ModelCallTiming(
    string Stage,
    string? Phase,
    double DurationMs,
    int InputTokens,
    int OutputTokens,
    int? FinishReasonLength);
