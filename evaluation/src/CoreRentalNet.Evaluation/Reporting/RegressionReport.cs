namespace CoreRentalNet.Evaluation.Reporting;

/// <summary>The comparison of a current run with an accepted baseline.</summary>
public sealed record RegressionReport(
    bool Comparable,
    int BaselinePassingRows,
    IReadOnlyList<CaseRegression> Regressions,
    double RegressionRate,
    IReadOnlyList<int> NewlyPassing)
{
    public bool HasRegressions => Regressions.Count > 0;

    /// <summary>Why an incomparable pair was refused, when AgentEval named the reasons.</summary>
    public IReadOnlyList<string> Reasons { get; init; } = [];

    /// <summary>Cases whose final setup differs from the baseline snapshot.</summary>
    public IReadOnlyList<SnapshotDrift> SnapshotChanges { get; init; } = [];
}
