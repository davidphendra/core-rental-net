using Microsoft.Extensions.AI.Evaluation;

using CoreRentalNet.Evaluation.Evaluators.Judges;
using CoreRentalNet.Evaluation.Evaluators.Correctness;
namespace CoreRentalNet.Evaluation.Evaluators.Shared;

/// <summary>One judge score with its explanation, kept so a human can review the decision later.</summary>
public sealed record JudgeScore(
    string Metric,
    double? Value,
    string Explanation,
    bool Evaluable,
    string GroundTruthSource = "none")
{
    public static JudgeScore From(EvaluationResult result, string metricName)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (!result.TryGet<NumericMetric>(metricName, out var metric) || metric?.Value is null)
        {
            return new JudgeScore(metricName, null, "The judge returned no score for this metric.", false);
        }

        return new JudgeScore(metricName, metric.Value, metric.Reason ?? string.Empty, true);
    }
}
