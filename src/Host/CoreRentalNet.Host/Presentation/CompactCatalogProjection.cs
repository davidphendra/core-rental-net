using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Host.Presentation;

/// <summary>Projects the module's published view onto the fields a machine caller reads.</summary>
/// <remarks>
/// Derived from <see cref="ProductView"/> rather than listed by hand from the module, so a field added
/// to the record is a decision about this projection rather than something that silently appears in it
/// - or silently fails to.
/// </remarks>
internal static class CompactCatalogProjection
{
    /// <param name="page">The rows this answer carries.</param>
    /// <param name="total">How many rows matched, which is more than the page when it was capped.</param>
    /// <param name="currency">The currency every price on the page is stated in.</param>
    public static CompactCatalogCollection Of(IReadOnlyList<ProductView> page, int total, string currency)
    {
        ArgumentNullException.ThrowIfNull(page);

        return new CompactCatalogCollection([.. page.Select(Item)], page.Count, total, currency);
    }

    /// <summary>One product as this projection carries it.</summary>
    /// <remarks>
    /// Exposed because the suggestion payload carries the same projection, and a second copy of this
    /// mapping is how the two would come to disagree about what the agent is told and what a caller reads.
    /// </remarks>
    public static CompactCatalogItem Item(ProductView product)
    {
        ArgumentNullException.ThrowIfNull(product);

        return new CompactCatalogItem(
            product.Sku,
            product.Name,
            product.Category,
            product.SubCategory,
            product.MonthlyPrice.Amount,
            product.Description,
            product.Metadata);
    }

    /// <summary>The currency a catalogue's prices are stated in, which an envelope states once.</summary>
    /// <remarks>
    /// Read from the catalogue rather than written here, and from the whole of it rather than from the rows
    /// that matched: an empty result still has a currency, and a filtered result must not decide what it is.
    /// The loader refuses an empty file and refuses a row priced in anything but the settlement currency, so
    /// the fallback is unreachable - it is there so that a broken invariant is answered rather than thrown
    /// at a caller.
    /// </remarks>
    public static string CurrencyOf(IReadOnlyList<ProductView> catalogue)
    {
        ArgumentNullException.ThrowIfNull(catalogue);

        return catalogue.Count == 0 ? Currencies.Idr : catalogue[0].MonthlyPrice.Currency;
    }
}
