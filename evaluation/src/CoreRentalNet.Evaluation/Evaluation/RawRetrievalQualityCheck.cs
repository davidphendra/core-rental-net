using CoreRentalNet.Evaluation.Evidence;
using Microsoft.Agents.AI;

namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>The raw-recall gate, expressed as a built-in MAF evaluation check.</summary>
public static class RawRetrievalQualityCheck
{
    public static EvalCheck Create()
        => FunctionEvaluator.Create(
            MetricNames.RawRetrievalRecall,
            (EvalItem item) => Evaluate(item));

    private static EvalCheckResult Evaluate(EvalItem item)
    {
        var result = RawRetrievalQuality.Evaluate(EvaluationContext.Parse(item.Context));

        return new EvalCheckResult(
            result.SelectedRecall >= 1,
            result.SelectedRecall >= 1
                ? "Every expected SKU was returned by a search."
                : string.Join("; ", result.FailureCodes),
            MetricNames.RawRetrievalRecall);
    }
}
