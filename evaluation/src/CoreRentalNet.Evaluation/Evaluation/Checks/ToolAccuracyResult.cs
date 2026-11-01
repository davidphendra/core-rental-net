using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;

namespace CoreRentalNet.Evaluation.Evaluation.Checks;

/// <summary>The tool result: component coverage and the calls that were missing or invalid.</summary>
public sealed record ToolAccuracyResult(
    bool Accurate,
    double Coverage,
    IReadOnlyList<string> FailureCodes);
