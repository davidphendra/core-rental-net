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
}
