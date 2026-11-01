using AgentEval.Comparison;

using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
namespace CoreRentalNet.Evaluation.Evaluation.Stochastic;

/// <summary>One metric's AgentEval stochastic statistics over the collected repetitions.</summary>
public sealed record StochasticMetricSummary(
    string Metric,
    int Runs,
    int Measured,
    int NotMeasured,
    int Passed,
    double? PassRate,
    double? Mean,
    double? Median,
    double? StandardDeviation,
    double? P25,
    double? P75,
    double? P95,
    ConfidenceInterval? ConfidenceInterval,
    StochasticVerdict Verdict);
