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

    /// <summary>Every filter is optional; an empty search narrows nothing. A term matches a
    /// product's name only.</summary>
    IReadOnlyList<ProductView> Search(CatalogCategory? category, CatalogSubCategory? subCategory, string? search);
}
