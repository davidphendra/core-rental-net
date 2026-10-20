namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>One metric value for one execution.</summary>
public sealed record MetricRow(
    string RunId,
    int RowNumber,
    int Repetition,
    string Provider,
    string Metric,
    bool Passed,
    double? Value,
    string? Reason);
