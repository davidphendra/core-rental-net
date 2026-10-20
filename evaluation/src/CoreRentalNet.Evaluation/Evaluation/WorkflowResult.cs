namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>The workflow result: one terminal status and the transitions that produced it.</summary>
public sealed record WorkflowResult(
    bool Correct,
    IReadOnlyList<string> FailureCodes);
