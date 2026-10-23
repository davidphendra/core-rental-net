using CoreRentalNet.Evaluation.Evidence;
using Microsoft.Agents.AI;

namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>The reranker-authority gate, expressed as a built-in MAF evaluation check.</summary>
public static class RerankerAlignmentCheck
{
    public static EvalCheck Create()
        => FunctionEvaluator.Create(
            MetricNames.RerankerAligned,
            (EvalItem item) => Evaluate(item));

    private static EvalCheckResult Evaluate(EvalItem item)
    {
        var result = RerankerAlignment.Evaluate(EvaluationContext.Parse(item.Context));

        return new EvalCheckResult(
            result.Aligned,
            result.Aligned
                ? $"Reranker returned supplied SKUs: {string.Join(", ", result.AlignedSlots)}"
                : "The reranker returned no aligned SKU.",
            MetricNames.RerankerAligned);
    }
}
