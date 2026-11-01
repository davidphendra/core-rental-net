using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;

namespace CoreRentalNet.Evaluation.Evaluation.Checks;

/// <summary>Whether every approved candidate respects the workspace rules.</summary>
public sealed record ConstraintResult(
    bool Compliant,
    IReadOnlyList<string> FailureCodes);
