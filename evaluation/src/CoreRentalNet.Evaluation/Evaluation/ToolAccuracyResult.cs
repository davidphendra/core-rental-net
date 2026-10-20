namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>The tool result: component coverage and the calls that were missing or invalid.</summary>
public sealed record ToolAccuracyResult(
    bool Accurate,
    double Coverage,
    IReadOnlyList<string> FailureCodes);
