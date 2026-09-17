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
    public static CompactCatalogCollection Of(IReadOnlyList<ProductView> products, string currency)
    {
        ArgumentNullException.ThrowIfNull(products);

        return new CompactCatalogCollection([.. products.Select(Item)], products.Count, currency);
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
