using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;

namespace CoreRentalNet.Evaluation.Evaluation.Stochastic;

/// <summary>The AgentEval stochastic view of one run's repetitions.</summary>
public sealed record StochasticSummary(
    double SuccessRateThreshold,
    IReadOnlyList<StochasticMetricSummary> Metrics);
