using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;
using CoreRentalNet.Evaluation.Metrics.Outcome;
using CoreRentalNet.Evaluation.Metrics.Retrieval;
using CoreRentalNet.Evaluation.Metrics.Statistics;
using CoreRentalNet.Evaluation.Metrics.Rules;
using CoreRentalNet.Evaluation.Metrics.Thresholds;

using CoreRentalNet.Evaluation.Runs.Record;
using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Assembly;
namespace CoreRentalNet.Evaluation.Runs.Result;

/// <summary>Imports the AgentEval RAG metrics into the run record, so one report carries every score.</summary>
/// <remarks>
/// The old pipeline owns the judge; this file only translates its rows. A metric the pipeline did not measure is
/// dropped rather than imported as zero, because a judge that failed to answer never scored the case.
/// </remarks>
public static class AgentEvalMetricImport
{
    private static readonly Dictionary<string, string> Names = new(StringComparer.Ordinal)
    {
        [MetricNames.LlmFaithfulness] = "faithfulness",
        [MetricNames.LlmContextPrecision] = "context_precision",
        [MetricNames.LlmContextRecall] = "context_recall",
    };

    public static IReadOnlyList<EvaluationMetricResult> Import(IReadOnlyList<MetricRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var imported = new List<EvaluationMetricResult>();

        foreach (var group in rows.GroupBy(row => row.Metric, StringComparer.Ordinal))
        {
            if (!Names.TryGetValue(group.Key, out var name))
            {
                continue;
            }

            var measured = group
                .Where(row => row.Measured && row.Value is not null)
                .Select(row => row.Value!.Value)
                .ToArray();

            if (measured.Length > 0)
            {
                imported.Add(MetricEstimate.FromValues(name, measured, group.Count() - measured.Length));
            }
        }

        return imported;
    }
}
