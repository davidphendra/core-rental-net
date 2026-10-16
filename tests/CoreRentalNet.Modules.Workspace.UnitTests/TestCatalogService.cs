using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Modules.Workspace.UnitTests;

/// <summary>A hand-written read source. No mocking library is used anywhere in this project.</summary>
internal sealed class TestCatalogService : IProductCatalogService
{
    private readonly Dictionary<string, ProductDto> views = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<ProductDto> All => views.Values.ToArray();

    public TestCatalogService Add(
        string sku,
        decimal monthlyPrice,
        CatalogCategory category = CatalogCategory.Accessory,
        CatalogSubCategory? subCategory = CatalogSubCategory.Monitor,
        string? name = null,
        bool imageAvailable = true)
    {
        views[sku] = new ProductDto(
            sku.ToUpperInvariant(),
            name ?? $"Product {sku.ToUpperInvariant()}",
            category,
            subCategory,
            new Money(monthlyPrice, Currencies.Idr),
            "A description.",
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

    public ProductDto? Find(string sku)
        => views.TryGetValue(sku, out var view) ? view : null;

    public IReadOnlyList<ProductDto> ByCategory(CatalogCategory category)
        => views.Values.Where(view => view.Category == category).ToArray();

    public IReadOnlyList<ProductDto> BySubCategory(CatalogSubCategory subCategory)
        => views.Values.Where(view => view.SubCategory == subCategory).ToArray();

    public IReadOnlyList<ProductDto> Featured()
        => views.Values.Where(view => view.IsFeatured).ToArray();

    public IReadOnlyList<ProductDto> Search(
        CatalogCategory? category,
        CatalogSubCategory? subCategory,
        decimal? maximumMonthlyAmount = null)
        => views.Values
            .Where(view => (category is null || view.Category == category)
                && (subCategory is null || view.SubCategory == subCategory))
            .ToArray();

    public Task<ProductDto?> FindAsync(string sku, CancellationToken cancellationToken)
        => Task.FromResult(Find(sku));

    public Task<IReadOnlyList<ProductDto>> FeaturedAsync(CancellationToken cancellationToken)
        => Task.FromResult(Featured());

    public Task<CatalogPage> SearchPageAsync(
        CatalogScope scope,
        decimal? maximumMonthlyAmount,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var matched = Search(scope.Category, scope.SubCategory, maximumMonthlyAmount);

        return Task.FromResult(new CatalogPage([.. matched.Skip(skip).Take(take)], matched.Count));
    }

    public Task<string> CurrencyAsync(CancellationToken cancellationToken)
        => Task.FromResult(views.Count == 0 ? Currencies.Idr : views.Values.First().MonthlyPrice.Currency);
}
