using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;

namespace CoreRentalNet.Evaluation.Evaluation.Checks;

/// <summary>The answer-correctness result: the exact gate plus the near-miss score.</summary>
public sealed record AnswerResult(
    bool Exact,
    double F1,
    IReadOnlyList<string> FailureCodes);
