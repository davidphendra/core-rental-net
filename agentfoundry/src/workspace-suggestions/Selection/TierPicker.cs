using AgentFoundry.WorkspaceSuggestions.Catalogue;

namespace AgentFoundry.WorkspaceSuggestions.Selection;

/// <summary>
/// Picks the product at a tier's position among a slot's candidates.
/// </summary>
/// <remarks>
/// <para>
/// Cheapest, median index, most expensive - in that order, with ties broken by SKU so the answer is
/// reproducible. Chosen over terciles or equal price bands because it gives the widest spread between
/// the three options and because it involves no convention a reader has to agree to: measured across
/// the catalogue it produces a 2.10x spread at the workspace level, where terciles give 1.73x.
/// </para>
/// <para>
/// The median depends on how many candidates there are, not only on which: measured on the monitors,
/// eight give 300/400/475 and five give 300/350/400. That is why the page's truncation is checked
/// before anything is tiered.
/// </para>
/// </remarks>
internal static class TierPicker
{
    /// <summary>How many candidates a slot needs before its tiers mean anything.</summary>
    public const int DistinctTiers = 3;

    /// <summary>The candidates in the order the positions are read from.</summary>
    public static IReadOnlyList<CatalogueItem> Ordered(IEnumerable<CatalogueItem> candidates)
        => [.. candidates.OrderBy(item => item.PricePerMonth).ThenBy(item => item.Sku, StringComparer.Ordinal)];

    /// <summary>True when a slot has too few candidates for its tiers to differ.</summary>
    /// <remarks>
    /// A pinned slot is filled by the same product in every option and is disclosed as such: with two
    /// candidates the low and middle tiers are the same product, and with one they all are.
    /// </remarks>
    public static bool IsPinned(IReadOnlyList<CatalogueItem> ordered)
        => ordered.Count < DistinctTiers;

    /// <summary>The candidate at a tier's position.</summary>
    public static CatalogueItem At(IReadOnlyList<CatalogueItem> ordered, int position)
    {
        ArgumentNullException.ThrowIfNull(ordered);

        if (ordered.Count == 0)
        {
            throw new ArgumentException("A slot with no candidates has no tier to fill.", nameof(ordered));
        }

        return position switch
        {
            0 => ordered[0],
            _ when position >= Tier.All.Count - 1 => ordered[^1],
            _ => ordered[ordered.Count / 2],
        };
    }
}
