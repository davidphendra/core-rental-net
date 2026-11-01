using CoreRentalNet.Evaluation.Runs.Record;
using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Result;
using CoreRentalNet.Evaluation.Runs.Assembly;

using CoreRentalNet.Evaluation.Reporting.Html;
using CoreRentalNet.Evaluation.Reporting.Writers;
using CoreRentalNet.Evaluation.Reporting.AgentEval;
namespace CoreRentalNet.Evaluation.Reporting.Comparison;

/// <summary>Compares a current run with a baseline, stating first whether the comparison is fair.</summary>
public static class RunComparer
{
    public static RunComparisonReport Compare(EvaluationRunRecord baseline, EvaluationRunRecord current)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(current);

        var currentScores = current.Metrics.ToDictionary(metric => metric.Metric, StringComparer.Ordinal);

        return new RunComparisonReport(
            Incompatibilities: [.. Incompatibilities(baseline, current)],
            Metrics:
            [
                .. baseline.Metrics
                    .Where(metric => currentScores.ContainsKey(metric.Metric))
                    .Select(metric => new MetricDelta(
                        metric.Metric,
                        metric.Score,
                        currentScores[metric.Metric].Score,
                        metric.Score is null || currentScores[metric.Metric].Score is null
                            ? null
                            : currentScores[metric.Metric].Score - metric.Score)),
            ]);
    }

    private static IEnumerable<string> Incompatibilities(
        EvaluationRunRecord baseline,
        EvaluationRunRecord current)
    {
        if (!string.Equals(baseline.DatasetSha256, current.DatasetSha256, StringComparison.Ordinal))
        {
            yield return "The dataset changed between the runs; the deltas compare different cases.";
        }

        if (!string.Equals(baseline.JudgeModel, current.JudgeModel, StringComparison.Ordinal))
        {
            yield return "The judge model changed between the runs; judge-scored metrics are not comparable.";
        }

        if (!string.Equals(baseline.GeneratorModel, current.GeneratorModel, StringComparison.Ordinal))
        {
            yield return "The generator model changed between the runs; this is a model comparison, not a regression.";
        }
    }
}
