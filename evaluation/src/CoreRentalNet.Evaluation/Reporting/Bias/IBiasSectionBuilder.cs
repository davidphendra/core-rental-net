namespace CoreRentalNet.Evaluation.Reporting.Bias;

/// <summary>Calculates one bias report section.</summary>
public interface IBiasSectionBuilder
{
    BiasSectionName Section { get; }

    IEnumerable<BiasMetric> Build(BiasInput input);
}
