using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;

namespace CoreRentalNet.Evaluation.Evaluation.Checks;

/// <summary>The workflow result: one terminal status and the transitions that produced it.</summary>
public sealed record WorkflowResult(
    bool Correct,
    IReadOnlyList<string> FailureCodes);
