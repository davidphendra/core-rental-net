using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Loading;
using CoreRentalNet.Evaluation.Datasets.Validation;
using CoreRentalNet.Evaluation.Runs.Record;
using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Result;
using CoreRentalNet.Evaluation.Runs.Assembly;

using CoreRentalNet.Evaluation.Analysis.Generalization;
namespace CoreRentalNet.Evaluation.Analysis.Category;

/// <summary>Every metric the record already supports, split by category.</summary>
/// <remarks>
/// Nothing new is persisted to produce this: the values are read from the same case records the report already
/// holds, so a category rate can never disagree with the run-level rate it is a part of.
/// </remarks>
public static class CategoryMetricPerformance
{
    public static IReadOnlyList<CategoryMetrics> Evaluate(EvaluationRunRecord run)
    {
        ArgumentNullException.ThrowIfNull(run);

        return
        [
            .. run.Cases
                .GroupBy(testCase => testCase.Category, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => Summarise(group.Key, [.. group], run.Judgements)),
        ];
    }

    private static CategoryMetrics Summarise(
        string category,
        IReadOnlyList<CaseRunRecord> cases,
        IReadOnlyList<CaseJudgement> judgements)
    {
        var ids = cases.Select(testCase => testCase.CaseId).ToHashSet(StringComparer.Ordinal);
        var values = new Dictionary<string, double?>(StringComparer.Ordinal)
        {
            ["outcome_accuracy"] = Mean(cases.Where(c => c.Actual != RagOutcome.Indeterminate)
                .Select(c => c.Expected == c.Actual ? 1d : 0d)),
            ["constraint_compliance"] = Mean(cases.Select(c =>
                string.Equals(c.FailedMetric, "constraint_compliance", StringComparison.Ordinal) ? 0d : 1d)),
            ["answer_correctness"] = Mean(cases.Select(c => c.Correct ? 1d : 0d)),
        };

        foreach (var group in judgements.Where(j => ids.Contains(j.CaseId)).GroupBy(j => j.Metric, StringComparer.Ordinal))
        {
            values[group.Key] = Mean(group.Where(j => j.Value is not null).Select(j => j.Value!.Value));
        }

        return new CategoryMetrics(category, cases.Count, values);
    }

    private static double? Mean(IEnumerable<double> values)
    {
        var array = values.ToArray();
        return array.Length == 0 ? null : array.Average();
    }
}

/// <summary>One category's metric values and the sample each was measured over.</summary>
public sealed record CategoryMetrics(string Category, int Cases, IReadOnlyDictionary<string, double?> Values);
