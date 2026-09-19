using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Host.Tests;

/// <summary>A hand-written catalogue, as this repository does everywhere instead of a mocking library.</summary>
/// <remarks>
/// Only what the suggestion payload needs: a run reads the whole catalogue and the currency it is priced in,
/// and looks nothing up. The richer read side is exercised by the catalogue's own tests, and a double that
/// implements more than the caller uses is a double that has to be maintained for nothing.
/// </remarks>
internal sealed class TestCatalogue : IProductCatalog
{
    private readonly List<ProductView> _products = [];

    public IReadOnlyList<ProductView> All => _products;

    public TestCatalogue Add(
        string sku,
        decimal monthlyPrice,
        CatalogCategory category = CatalogCategory.Desk,
        CatalogSubCategory? subCategory = null)
    {
        _products.Add(new ProductView(
            sku,
            $"Product {sku}",
            category,
            subCategory,
            new Money(monthlyPrice, Currencies.Idr),
            "A description.",
            new CatalogMetadata([], new Dictionary<string, string>(), [], []),
            "/images/test.svg",
            true,
            false));

        return this;
    }

    public ProductView? Find(string sku)
        => _products.FirstOrDefault(product => string.Equals(product.Sku, sku, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<ProductView> ByCategory(CatalogCategory category)
        => [.. _products.Where(product => product.Category == category)];

    public IReadOnlyList<ProductView> BySubCategory(CatalogSubCategory subCategory)
        => [.. _products.Where(product => product.SubCategory == subCategory)];

    public IReadOnlyList<ProductView> Featured()
        => [.. _products.Where(product => product.IsFeatured)];

    public IReadOnlyList<ProductView> Search(CatalogCategory? category, CatalogSubCategory? subCategory, string? search)
        => [.. _products.Where(product =>
            (category is null || product.Category == category)
            && (subCategory is null || product.SubCategory == subCategory)
            && (string.IsNullOrWhiteSpace(search)
                || product.Name.Contains(search, StringComparison.OrdinalIgnoreCase)))];
}
