using AgentEval.Comparison;
using CoreRentalNet.Evaluation.Runs.Record;
using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Result;
using CoreRentalNet.Evaluation.Runs.Assembly;

using CoreRentalNet.Evaluation.Metrics.Outcome;
using CoreRentalNet.Evaluation.Metrics.Retrieval;
using CoreRentalNet.Evaluation.Metrics.Rules;
using CoreRentalNet.Evaluation.Metrics.Thresholds;
namespace CoreRentalNet.Evaluation.Metrics.Statistics;

/// <summary>Turns per-case values into a stored metric with AgentEval's interval and its sample size.</summary>
/// <remarks>
/// A mean without a sample size invites a reader to trust two cases as much as two hundred. Every metric this
/// factory produces carries its own n, its excluded count and the interval the pinned calculator computed.
/// </remarks>
public static class MetricEstimate
{
    public const double ConfidenceLevel = 0.95;
    public const int MinimumSampleSize = 30;

    public static EvaluationMetricResult FromValues(
        string metric,
        IReadOnlyList<double> values,
        int excluded)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (values.Count == 0)
        {
            return new EvaluationMetricResult(metric, null, 0, excluded, ConfidenceLevel, null, null, "insufficient sample");
        }

        var mean = values.Average();
        var interval = values.Count < 2
            ? null
            : DefaultStatisticsCalculator.Instance.CalculateConfidenceInterval(values, ConfidenceLevel);

        return new EvaluationMetricResult(
            metric,
            mean,
            values.Count,
            excluded,
            ConfidenceLevel,
            interval?.Lower,
            interval?.Upper,
            values.Count >= MinimumSampleSize ? "evaluable" : "insufficient sample");
    }
}
