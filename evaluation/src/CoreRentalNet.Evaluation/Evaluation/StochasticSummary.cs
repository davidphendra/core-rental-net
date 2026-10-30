namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>The AgentEval stochastic view of one run's repetitions.</summary>
public sealed record StochasticSummary(
    double SuccessRateThreshold,
    IReadOnlyList<StochasticMetricSummary> Metrics);
