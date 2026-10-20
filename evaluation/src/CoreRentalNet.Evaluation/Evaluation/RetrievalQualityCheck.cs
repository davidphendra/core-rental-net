using CoreRentalNet.Evaluation.Evidence;
using Microsoft.Agents.AI;

namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>The selected-recall gate, expressed as a built-in MAF evaluation check.</summary>
public static class RetrievalQualityCheck
{
    public static EvalCheck Create()
        => FunctionEvaluator.Create(
            MetricNames.RetrievalQuality,
            (EvalItem item) => Evaluate(item));

    private static EvalCheckResult Evaluate(EvalItem item)
    {
        var result = RetrievalQuality.Evaluate(EvaluationContext.Parse(item.Context));

        return new EvalCheckResult(
            result.SelectedRecall >= 1,
            result.SelectedRecall >= 1
                ? "Every expected SKU was selected."
                : string.Join("; ", result.FailureCodes),
            MetricNames.RetrievalQuality);
    }
}
