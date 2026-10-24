using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Modules.Discovery.Infrastructure.NameSearch;

/// <summary>Fuses per-term result lists without letting one broad term consume the answer.</summary>
/// <remarks>
/// Reciprocal rank fusion is used because Lifti's BM25 scores are produced per term and are not
/// comparable across different term queries. Each term contributes its own ranking, so a specific
/// term still reaches the caller after the handler applies its result cap.
/// </remarks>
internal static class RankedTermMerger
{
    /// <summary>The candidates each term contributes before fusion.</summary>
    public const int MaximumPerTerm = 25;

    private const double RankConstant = 60;

    public static IReadOnlyList<ProductDto> Merge(
        IReadOnlyList<IReadOnlyList<ProductDto>> termResults)
    {
        var scores = new Dictionary<string, (ProductDto Product, double Score)>(StringComparer.OrdinalIgnoreCase);

        foreach (var results in termResults)
        {
            for (var index = 0; index < results.Count; index++)
            {
                var product = results[index];
                var score = 1d / (RankConstant + index + 1);

                if (scores.TryGetValue(product.Sku, out var existing))
                {
                    scores[product.Sku] = (existing.Product, existing.Score + score);
                }
                else
                {
                    scores[product.Sku] = (product, score);
                }
            }
        }

        return [.. scores.Values
            .OrderByDescending(entry => entry.Score)
            .ThenBy(entry => entry.Product.Sku, StringComparer.Ordinal)
            .Select(entry => entry.Product)];
    }
}
