using CoreRentalNet.Evaluation.Evidence;

namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>Measures whether every expected SKU appeared in any catalogue answer.</summary>
/// <remarks>
/// This is deliberately separate from <see cref="RetrievalQuality"/>, which measures the reranker's selected
/// set. A product can be returned by a search and then dropped by the reranker; one metric calls that a retrieval
/// failure and the other calls it a selection failure.
/// </remarks>
public static class RawRetrievalQuality
{
    public static RetrievalResult Evaluate(EvaluationContext context)
    {
        var returned = context.FinalToolAnswers
            .SelectMany(answer => answer.ReturnedSkus)
            .ToHashSet(StringComparer.Ordinal);

        var recall = context.ExpectedVariants.Count == 0
            ? 0
            : context.ExpectedVariants.Max(variant =>
                variant.Skus.Count == 0
                    ? 1
                    : variant.Skus.Count(returned.Contains) / (double)variant.Skus.Count);

        if (recall >= 1)
        {
            return new RetrievalResult(1, []);
        }

        var best = context.ExpectedVariants
            .OrderByDescending(variant => variant.Skus.Count(returned.Contains))
            .First();

        return new RetrievalResult(
            recall,
            [.. best.Skus
                .Where(sku => !returned.Contains(sku))
                .Select(FailureCodes.RetrievalMiss)]);
    }
}
