using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Discovery.Application.Shortlist;

namespace CoreRentalNet.Host.Tests;

/// <summary>A shortlist a test decides, and the record of what it was asked for.</summary>
/// <remarks>
/// The queries are kept because the sentence's journey is part of what a run is: retrieval is asked for the
/// customer's own words, and a test that watched nothing would not notice a trimmed or rewritten one.
/// </remarks>
internal sealed class StubCatalogShortlist(params string[] skus) : ICatalogShortlist
{
    /// <summary>Every query this stub was asked for, in order.</summary>
    public List<string> Queries { get; } = [];

    /// <summary>The items the same SKUs would produce, for a test that needs the list rather than the port.</summary>
    /// <remarks>
    /// The payload builder takes the items because it is a projection and fetches nothing, so a test of it needs
    /// the list and not the operation. Built by the same code the operation uses, so the two cannot disagree.
    /// </remarks>
    public static IReadOnlyList<ShortlistItem> Items(params string[] skus)
        => [.. skus.Select((sku, position) => new ShortlistItem(
            sku,
            new CatalogBucket(CatalogCategory.Desk, null),
            1f - (position / 100f)))];

    public Task<IReadOnlyList<ShortlistItem>> ForAsync(string query, CancellationToken cancellationToken)
    {
        Queries.Add(query);

        // Nearest first, so a caller that reverses the order is visible.
        return Task.FromResult(Items(skus));
    }
}
