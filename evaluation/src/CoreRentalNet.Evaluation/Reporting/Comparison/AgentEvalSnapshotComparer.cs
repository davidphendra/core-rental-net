using System.Text.Json;
using AgentEval.Snapshots;
using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;

using CoreRentalNet.Evaluation.Reporting.Html;
using CoreRentalNet.Evaluation.Reporting.Writers;
using CoreRentalNet.Evaluation.Reporting.AgentEval;
namespace CoreRentalNet.Evaluation.Reporting.Comparison;

/// <summary>Compares each case's final setup through AgentEval's snapshot comparer.</summary>
public static class AgentEvalSnapshotComparer
{
    private static readonly SnapshotComparer Comparer = new(new SnapshotOptions
    {
        UseSemanticComparison = false,
        IgnoreFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "timestamp", "duration", "elapsed", "startTime", "endTime", "id", "requestId", "created",
        },
    });

    public static IReadOnlyList<SnapshotDrift> Compare(
        EvaluationReport baseline,
        EvaluationReport current)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(current);

        var baselineByRow = baseline.Cases.ToDictionary(testCase => testCase.RowNumber);
        var drifts = new List<SnapshotDrift>();

        foreach (var testCase in current.Cases)
        {
            if (!baselineByRow.TryGetValue(testCase.RowNumber, out var before))
            {
                continue;
            }

            var expected = Snapshot(before.ActualOutput);
            var actual = Snapshot(testCase.ActualOutput);
            var result = Comparer.Compare(expected, actual);

            if (!result.IsMatch)
            {
                drifts.Add(new SnapshotDrift(
                    testCase.RowNumber,
                    testCase.CaseId,
                    [.. result.Differences.Select(difference =>
                        $"{difference.Path}: {difference.Message}")]));
            }
        }

        return drifts;
    }

    private static string Snapshot(string output)
        => JsonSerializer.Serialize(new { response = output });
}
