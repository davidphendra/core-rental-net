namespace CoreRentalNet.Evaluation.Reporting.Bias;

/// <summary>Whether a bias metric could be calculated.</summary>
public enum BiasMetricStatus
{
    Computed,
    NotApplicable,
    InsufficientData,
}
