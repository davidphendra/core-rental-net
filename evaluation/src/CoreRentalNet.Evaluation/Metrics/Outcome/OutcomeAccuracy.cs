using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Loading;
using CoreRentalNet.Evaluation.Datasets.Validation;

using CoreRentalNet.Evaluation.Metrics.Retrieval;
using CoreRentalNet.Evaluation.Metrics.Statistics;
using CoreRentalNet.Evaluation.Metrics.Rules;
using CoreRentalNet.Evaluation.Metrics.Thresholds;
namespace CoreRentalNet.Evaluation.Metrics.Outcome;

/// <summary>Outcome accuracy, the confusion matrix, and each class's precision, recall and F1.</summary>
/// <remarks>
/// Accuracy is computed over determinate runs only. A run that reported no status is counted separately, because
/// treating "said nothing" as "said rejected" would flatter the application on exactly the failure it should show.
/// </remarks>
public static class OutcomeAccuracy
{
    private static readonly RagOutcome[] Classes =
        [RagOutcome.Success, RagOutcome.Rejected, RagOutcome.Unavailable];

    public static OutcomeAccuracyReport Evaluate(IReadOnlyList<OutcomeRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        var determinate = records.Where(record => record.IsDeterminate).ToArray();
        var correct = determinate.Count(record => record.IsCorrect);

        return new OutcomeAccuracyReport(
            Cases: records.Count,
            Determinate: determinate.Length,
            Indeterminate: records.Count - determinate.Length,
            Correct: correct,
            Accuracy: determinate.Length == 0 ? null : correct / (double)determinate.Length,
            Confusion: Confusion(records),
            Classes: [.. Classes.Select(label => Score(label, determinate))]);
    }

    private static IReadOnlyDictionary<string, int> Confusion(IReadOnlyList<OutcomeRecord> records)
        => records
            .GroupBy(record => $"{record.Expected}->{record.Actual}", StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

    private static OutcomeClassScore Score(RagOutcome label, IReadOnlyList<OutcomeRecord> determinate)
    {
        var truePositive = determinate.Count(record => record.Expected == label && record.Actual == label);
        var predicted = determinate.Count(record => record.Actual == label);
        var support = determinate.Count(record => record.Expected == label);

        var precision = predicted == 0 ? 0 : truePositive / (double)predicted;
        var recall = support == 0 ? 0 : truePositive / (double)support;
        var f1 = precision + recall == 0 ? 0 : 2 * precision * recall / (precision + recall);

        return new OutcomeClassScore(label.ToString(), support, precision, recall, f1);
    }
}

/// <summary>The outcome report a run stores and the HTML report renders.</summary>
public sealed record OutcomeAccuracyReport(
    int Cases,
    int Determinate,
    int Indeterminate,
    int Correct,
    double? Accuracy,
    IReadOnlyDictionary<string, int> Confusion,
    IReadOnlyList<OutcomeClassScore> Classes);
