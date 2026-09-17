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

    private static CompactCatalogItem Item(ProductView product)
        => new(
            product.Sku,
            product.Name,
            product.Category,
            product.SubCategory,
            product.MonthlyPrice.Amount,
            product.Description,
            product.Metadata);
}
