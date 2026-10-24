namespace CoreRentalNet.Evaluation.Reporting.Bias;

/// <summary>The A-D bias metrics for one evaluation run.</summary>
public sealed record BiasReport(IReadOnlyList<BiasMetric> Metrics);
