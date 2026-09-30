using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Catalog.Application.Contracts;

/// <summary>
/// The catalogue read port: how this module's application and the other modules read the catalogue.
/// </summary>
/// <remarks>
/// There is deliberately no write side: the catalogue is an immutable snapshot loaded from
/// <c>products.json</c> once, at start-up. It lives in memory, so this port is synchronous; an
/// adapter over a database or a network would need these operations to return a task, and would
/// change this contract and its callers.
/// </remarks>
public interface IProductCatalogService
{
    /// <summary>Every product, in catalog order.</summary>
    IReadOnlyList<ProductView> All { get; }

    /// <summary>One product by SKU, ignoring letter case, or null when the catalogue has none.</summary>
    ProductView? Find(string sku);

    /// <summary>Every product in one category, in catalog order.</summary>
    IReadOnlyList<ProductView> ByCategory(CatalogCategory category);

    /// <summary>Every product in one subcategory, in catalog order.</summary>
    IReadOnlyList<ProductView> BySubCategory(CatalogSubCategory subCategory);

    /// <summary>The products carrying the popular badge.</summary>
    IReadOnlyList<ProductView> Featured();

    /// <summary>Every product the eligibility rules allow, in catalog order. What a caller typed does not narrow
    /// here: it ranks the answer — see <see cref="IProductNameSearchService"/>.</summary>
    /// <param name="category">The category to narrow to, or null for all of them.</param>
    /// <param name="subCategory">The accessory subcategory to narrow to, or null for all of them.</param>
    /// <param name="maximumMonthlyAmount">The most a product may cost each month, or null for no ceiling.
    /// A product above it is not eligible, so a good name match can never bring it back.</param>
    /// <remarks>
    /// <b>The ceiling narrows rather than ranking.</b> It is here, beside the category, rather than beside
    /// what was typed, because a product a caller cannot afford is not a product to be ordered lower — it is
    /// not a product. Both name searches narrow through this one operation, which is what stops the two of
    /// them disagreeing about what is eligible.
    /// </remarks>
    IReadOnlyList<ProductView> Search(
        CatalogCategory? category,
        CatalogSubCategory? subCategory,
        decimal? maximumMonthlyAmount = null);
}
