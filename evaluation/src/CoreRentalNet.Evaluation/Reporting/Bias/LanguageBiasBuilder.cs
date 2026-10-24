namespace CoreRentalNet.Evaluation.Reporting.Bias;

/// <summary>Reports language and content bias metrics that require labels or classifiers.</summary>
public sealed class LanguageBiasBuilder : IBiasSectionBuilder
{
    private const string MissingGroups = "No protected-group labels exist in the fixture.";

    public BiasSectionName Section => BiasSectionName.Language;

    public IEnumerable<BiasMetric> Build(BiasInput input)
    {
        yield return BiasMetric.NotApplicable(
            Section,
            "Stereotype Incidence Rate",
            "No stereotype annotation or classifier exists in the evaluation harness.");
        yield return BiasMetric.NotApplicable(
            Section,
            "Toxicity Rate by Group",
            "No toxicity classifier or protected-group labels exist in the evaluation harness.");
        yield return BiasMetric.NotApplicable(
            Section,
            "Sentiment Disparity",
            "No equivalent prompt pairs or sentiment classifier exist in the fixture.");
        yield return BiasMetric.NotApplicable(Section, "Refusal Rate by Group", MissingGroups);
        yield return BiasMetric.NotApplicable(Section, "Quality-of-Service Gap", MissingGroups);
        yield return BiasMetric.NotApplicable(
            Section,
            "Uncertainty Calibration Gap",
            "No protected-group labels or per-group confidence values exist in the fixture.");
    }
}
