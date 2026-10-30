namespace CoreRentalNet.Evaluation.Reporting;

/// <summary>One case whose final setup differs from the baseline snapshot.</summary>
public sealed record SnapshotDrift(
    int RowNumber,
    string CaseId,
    IReadOnlyList<string> Differences);
