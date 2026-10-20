namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>Whether every approved candidate respects the workspace rules.</summary>
public sealed record ConstraintResult(
    bool Compliant,
    IReadOnlyList<string> FailureCodes);
