using CoreRentalNet.Evaluation.Runs.Record;
using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Result;
using CoreRentalNet.Evaluation.Runs.Assembly;

using CoreRentalNet.Evaluation.Analysis.Category;
namespace CoreRentalNet.Evaluation.Analysis.Generalization;

/// <summary>Computes the gap only for metrics both runs actually measured.</summary>
public static class GeneralizationGapCalculator
{
    public static GeneralizationGapReport Compare(EvaluationRunRecord development, EvaluationRunRecord test)
    {
        ArgumentNullException.ThrowIfNull(development);
        ArgumentNullException.ThrowIfNull(test);

        var testScores = test.Metrics.ToDictionary(metric => metric.Metric, StringComparer.Ordinal);

        var gaps = development.Metrics
            .Where(metric => testScores.ContainsKey(metric.Metric))
            .Select(metric => new GeneralizationGap(
                metric.Metric,
                metric.Score,
                testScores[metric.Metric].Score))
            .ToArray();

        return new GeneralizationGapReport(gaps, GeneralizationGapReport.StandardCaveat);
    }
}
