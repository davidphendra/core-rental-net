using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Modules.Workspace.UnitTests;

/// <summary>A hand-written read source. No mocking library is used anywhere in this project.</summary>
internal sealed class TestCatalogService : IProductCatalogService
{
    private readonly Dictionary<string, ProductView> views = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<ProductView> All => views.Values.ToArray();

    public TestCatalogService Add(
        string sku,
        decimal monthlyPrice,
        CatalogCategory category = CatalogCategory.Accessory,
        CatalogSubCategory? subCategory = CatalogSubCategory.Monitor,
        string? name = null,
        bool imageAvailable = true)
    {
        views[sku] = new ProductView(
            sku.ToUpperInvariant(),
            name ?? $"Product {sku.ToUpperInvariant()}",
            category,
            subCategory,
            new Money(monthlyPrice, Currencies.Idr),
            "A description.",
            new CatalogMetadata([], new Dictionary<string, string>(), [], []),
            "/images/test.svg",
            imageAvailable,
            false);

        return this;
    }

    public TestCatalogService Remove(string sku)
    {
        views.Remove(sku);
        return this;
    }

    public ProductView? Find(string sku)
        => views.TryGetValue(sku, out var view) ? view : null;

    public IReadOnlyList<ProductView> ByCategory(CatalogCategory category)
        => views.Values.Where(view => view.Category == category).ToArray();

    public IReadOnlyList<ProductView> BySubCategory(CatalogSubCategory subCategory)
        => views.Values.Where(view => view.SubCategory == subCategory).ToArray();

    public IReadOnlyList<ProductView> Featured()
        => views.Values.Where(view => view.IsFeatured).ToArray();

    public IReadOnlyList<ProductView> Search(
        CatalogCategory? category,
        CatalogSubCategory? subCategory,
        decimal? maximumMonthlyAmount = null)
        => views.Values
            .Where(view => (category is null || view.Category == category)
                && (subCategory is null || view.SubCategory == subCategory))
            .ToArray();
}
