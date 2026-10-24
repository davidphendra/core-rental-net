namespace CoreRentalNet.Evaluation.Reporting.Bias;

/// <summary>Reports protected-group fairness metrics when the fixture supplies labels.</summary>
public sealed class FairnessBiasBuilder : IBiasSectionBuilder
{
    private const string MissingGroups = "No protected-group labels exist in the fixture.";

    public BiasSectionName Section => BiasSectionName.Fairness;

    public IEnumerable<BiasMetric> Build(BiasInput input)
    {
        yield return BiasMetric.NotApplicable(Section, "Demographic Parity Difference", MissingGroups);
        yield return BiasMetric.NotApplicable(Section, "Disparate Impact Ratio", MissingGroups);
        yield return BiasMetric.NotApplicable(
            Section,
            "Equal Opportunity Difference",
            "No qualified labels or protected-group labels exist in the fixture.");
        yield return BiasMetric.NotApplicable(
            Section,
            "Equalized Odds Difference",
            "No protected-group labels or negative controls exist in the fixture.");
        yield return BiasMetric.NotApplicable(
            Section,
            "Group Calibration Error",
            "No protected-group labels or per-group confidence values exist in the fixture.");
        yield return BiasMetric.NotApplicable(Section, "Worst-Group Performance", MissingGroups);
    }
}
