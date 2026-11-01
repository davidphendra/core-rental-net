using CoreRentalNet.Evaluation.Evidence.Trace;
using CoreRentalNet.Evaluation.Evidence.Projection;
using CoreRentalNet.Evaluation.Evidence.Context;

using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;
namespace CoreRentalNet.Evaluation.Evaluation.Checks;

/// <summary>Measures whether every expected SKU reached the reranker's selected set.</summary>
public static class RetrievalQuality
{
    public static RetrievalResult Evaluate(EvaluationContext context)
    {
        var selected = context.SelectedSkus;

        var recall = context.ExpectedVariants.Count == 0
            ? 0
            : context.ExpectedVariants.Max(variant =>
                variant.Skus.Count == 0
                    ? 1
                    : variant.Skus.Count(selected.Contains) / (double)variant.Skus.Count);

        if (recall >= 1)
        {
            return new RetrievalResult(1, []);
        }

        var best = context.ExpectedVariants
            .OrderByDescending(variant => variant.Skus.Count(selected.Contains))
            .First();

        return new RetrievalResult(
            recall,
            [.. best.Skus
                .Where(sku => !selected.Contains(sku))
                .Select(FailureCodes.RetrievalMiss)]);
    }
}
