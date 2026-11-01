using CoreRentalNet.Evaluation.Metrics.Outcome;
using CoreRentalNet.Evaluation.Metrics.Retrieval;
using CoreRentalNet.Evaluation.Metrics.Statistics;
using CoreRentalNet.Evaluation.Metrics.Rules;
using CoreRentalNet.Evaluation.Metrics.Thresholds;

using CoreRentalNet.Evaluation.Analysis.Generalization;
namespace CoreRentalNet.Evaluation.Analysis.Category;

/// <summary>Outcome accuracy per category, so a weak category cannot hide inside an overall average.</summary>
public static class CategoryOutcomePerformance
{
    public static IReadOnlyList<CategoryOutcomeResult> Evaluate(IReadOnlyList<OutcomeRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        return
        [
            .. records
                .GroupBy(record => record.Category, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => Summarise(group.Key, [.. group])),
        ];
    }

    private static CategoryOutcomeResult Summarise(string category, IReadOnlyList<OutcomeRecord> records)
    {
        var determinate = records.Where(record => record.IsDeterminate).ToArray();
        var correct = determinate.Count(record => record.IsCorrect);

        return new CategoryOutcomeResult(
            Category: category,
            Cases: records.Count,
            Determinate: determinate.Length,
            Indeterminate: records.Count - determinate.Length,
            Correct: correct,
            Accuracy: determinate.Length == 0 ? null : correct / (double)determinate.Length);
    }
}

/// <summary>One category's outcome accuracy and the sample it rests on.</summary>
public sealed record CategoryOutcomeResult(
    string Category,
    int Cases,
    int Determinate,
    int Indeterminate,
    int Correct,
    double? Accuracy);
