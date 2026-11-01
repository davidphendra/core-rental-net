using AgentEval.Comparison;

using CoreRentalNet.Evaluation.Metrics.Outcome;
using CoreRentalNet.Evaluation.Metrics.Retrieval;
using CoreRentalNet.Evaluation.Metrics.Rules;
using CoreRentalNet.Evaluation.Metrics.Thresholds;
namespace CoreRentalNet.Evaluation.Metrics.Statistics;

/// <summary>Whether an estimate may be read as evidence, or merely as a number.</summary>
public enum ProportionEstimateStatus
{
    Evaluable,
    InsufficientSample,
}

/// <summary>A case-level proportion with AgentEval's confidence interval and its sample size.</summary>
/// <remarks>
/// The interval comes from the pinned AgentEval calculator rather than a local formula, so one statistical
/// implementation decides what "95% confident" means across every metric in the harness.
/// </remarks>
public sealed record ProportionEstimate(
    string Metric,
    double Value,
    int SampleSize,
    int Excluded,
    double ConfidenceLevel,
    double Lower,
    double Upper,
    ProportionEstimateStatus Status)
{
    public static ProportionEstimate From(
        string metric,
        IReadOnlyList<bool> outcomes,
        int excluded,
        double confidenceLevel,
        int minimumSampleSize)
    {
        ArgumentNullException.ThrowIfNull(outcomes);
        ArgumentException.ThrowIfNullOrWhiteSpace(metric);
        ArgumentOutOfRangeException.ThrowIfNegative(excluded);

        if (outcomes.Count == 0)
        {
            return new ProportionEstimate(metric, 0, 0, excluded, confidenceLevel, 0, 1,
                ProportionEstimateStatus.InsufficientSample);
        }

        var scores = outcomes.Select(value => value ? 1d : 0d).ToArray();
        var value0 = scores.Average();
        var interval = Interval(scores, confidenceLevel);
        var status = outcomes.Count >= minimumSampleSize
            ? ProportionEstimateStatus.Evaluable
            : ProportionEstimateStatus.InsufficientSample;

        return new ProportionEstimate(
            metric, value0, outcomes.Count, excluded, confidenceLevel,
            interval.Lower, interval.Upper, status);
    }

    /// <summary>The interval, widened to the whole range when the sample cannot support one.</summary>
    private static ConfidenceInterval Interval(IReadOnlyList<double> scores, double confidenceLevel)
    {
        if (scores.Count < 2)
        {
            return new ConfidenceInterval(0, 1, confidenceLevel);
        }

        return DefaultStatisticsCalculator.Instance.CalculateConfidenceInterval(scores, confidenceLevel)
            ?? new ConfidenceInterval(0, 1, confidenceLevel);
    }
}
