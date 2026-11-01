using AgentEval.Comparison;

using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
namespace CoreRentalNet.Evaluation.Evaluation.Stochastic;

/// <summary>Builds stochastic statistics with AgentEval's calculator and verdict rule.</summary>
public static class StochasticSummaryFactory
{
    public static StochasticSummary Create(EvaluationReport report, double successRateThreshold)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (!double.IsFinite(successRateThreshold)
            || successRateThreshold < 0
            || successRateThreshold > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(successRateThreshold),
                successRateThreshold,
                "Expected a finite threshold between zero and one.");
        }

        var metrics = report.Metrics
            .GroupBy(row => row.Metric, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => Create(group.Key, group.ToArray(), successRateThreshold))
            .ToArray();

        return new StochasticSummary(successRateThreshold, metrics);
    }

    private static StochasticMetricSummary Create(
        string metric,
        IReadOnlyList<MetricRow> rows,
        double successRateThreshold)
    {
        var measured = rows.Where(row => row.Measured).ToArray();
        var scores = measured
            .Where(row => row.Value is not null)
            .Select(row => row.Value!.Value)
            .ToArray();
        var passed = measured.Count(row => row.Passed);
        var notMeasured = rows.Count - measured.Length;
        var calculator = DefaultStatisticsCalculator.Instance;
        var verdict = StochasticResult.DecideVerdict(
            passed,
            notMeasured,
            rows.Count,
            successRateThreshold);

        return new StochasticMetricSummary(
            Metric: metric,
            Runs: rows.Count,
            Measured: measured.Length,
            NotMeasured: notMeasured,
            Passed: passed,
            PassRate: rows.Count == 0 ? null : passed / (double)rows.Count,
            Mean: calculator.Mean(scores),
            Median: calculator.Median(scores),
            StandardDeviation: calculator.StandardDeviation(scores),
            P25: calculator.Percentile(scores, 25),
            P75: calculator.Percentile(scores, 75),
            P95: calculator.Percentile(scores, 95),
            ConfidenceInterval: calculator.CalculateConfidenceInterval(scores, 0.95),
            Verdict: verdict);
    }
}
