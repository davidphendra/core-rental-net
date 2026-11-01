using CoreRentalNet.Evaluation.Configuration;
using CoreRentalNet.Evaluation.Runs.Record;
using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Result;
using CoreRentalNet.Evaluation.Runs.Assembly;

using CoreRentalNet.Evaluation.Metrics.Outcome;
using CoreRentalNet.Evaluation.Metrics.Retrieval;
using CoreRentalNet.Evaluation.Metrics.Statistics;
using CoreRentalNet.Evaluation.Metrics.Rules;
namespace CoreRentalNet.Evaluation.Metrics.Thresholds;

/// <summary>Attaches each metric's configured pass line and verdict, keeping the score and the status untouched.</summary>
/// <remarks>
/// A metric whose sample is too small cannot pass, whatever it scored. Reporting "pass" on four cases is the
/// failure mode this guard exists for, so the verdict is withheld rather than granted.
/// </remarks>
public static class ThresholdPolicy
{
    public static EvaluationRunRecord Apply(EvaluationRunRecord record, EvaluationThresholds thresholds)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(thresholds);

        return record with
        {
            Metrics =
            [
                .. record.Metrics.Select(metric => Annotate(metric, thresholds.Minimums.GetValueOrDefault(metric.Metric, double.NaN))),
            ],
        };
    }

    private static EvaluationMetricResult Annotate(EvaluationMetricResult metric, double minimum)
    {
        if (double.IsNaN(minimum))
        {
            return metric;
        }

        var passed = metric.IsEvaluable && metric.Score is not null && metric.Score >= minimum;
        return metric with { Threshold = minimum, Passed = passed };
    }
}
