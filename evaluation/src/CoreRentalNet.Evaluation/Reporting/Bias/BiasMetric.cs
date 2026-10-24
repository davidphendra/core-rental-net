namespace CoreRentalNet.Evaluation.Reporting.Bias;

/// <summary>One bias metric, including why it could not be calculated.</summary>
public sealed record BiasMetric(
    BiasSectionName Section,
    string Name,
    BiasMetricStatus Status,
    double? Value,
    int SampleSize,
    string? Reason,
    string? Stage = null)
{
    public static BiasMetric Computed(
        BiasSectionName section,
        string name,
        double value,
        int sampleSize,
        string? stage = null)
        => new(section, name, BiasMetricStatus.Computed, value, sampleSize, null, stage);

    public static BiasMetric NotApplicable(
        BiasSectionName section,
        string name,
        string reason,
        string? stage = null)
        => new(section, name, BiasMetricStatus.NotApplicable, null, 0, reason, stage);

    public static BiasMetric Insufficient(
        BiasSectionName section,
        string name,
        string reason,
        int sampleSize,
        string? stage = null)
        => new(section, name, BiasMetricStatus.InsufficientData, null, sampleSize, reason, stage);
}
