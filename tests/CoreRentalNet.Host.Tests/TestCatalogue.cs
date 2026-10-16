using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Host.Tests;

/// <summary>A hand-written catalogue, as this repository does everywhere instead of a mocking library.</summary>
/// <remarks>
/// Only what the suggestion payload needs: a run reads the whole catalogue and the currency it is priced in,
/// and looks nothing up. The richer read side is exercised by the catalogue's own tests, and a double that
/// implements more than the caller uses is a double that has to be maintained for nothing.
/// </remarks>
internal sealed class TestCatalogue : IProductCatalogService
{
    private readonly List<ProductDto> _products = [];

    public IReadOnlyList<ProductDto> All => _products;

    public TestCatalogue Add(
        string sku,
        decimal monthlyPrice,
        CatalogCategory category = CatalogCategory.Desk,
        CatalogSubCategory? subCategory = null)
    {
        _products.Add(new ProductDto(
            sku,
            $"Product {sku}",
            category,
            subCategory,
            new Money(monthlyPrice, Currencies.Idr),
            "A description.",
            "/images/test.svg",
            true,
            false));

        return this;
    }

    public ProductDto? Find(string sku)
        => _products.FirstOrDefault(product => string.Equals(product.Sku, sku, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<ProductDto> ByCategory(CatalogCategory category)
        => [.. _products.Where(product => product.Category == category)];

    public IReadOnlyList<ProductDto> BySubCategory(CatalogSubCategory subCategory)
        => [.. _products.Where(product => product.SubCategory == subCategory)];

    public IReadOnlyList<ProductDto> Featured()
        => [.. _products.Where(product => product.IsFeatured)];

    public IReadOnlyList<ProductDto> Search(
        CatalogCategory? category,
        CatalogSubCategory? subCategory,
        decimal? maximumMonthlyAmount = null)
        => [.. _products.Where(product =>
            (category is null || product.Category == category)
            && (subCategory is null || product.SubCategory == subCategory)
            && (maximumMonthlyAmount is null || product.MonthlyPrice.Amount <= maximumMonthlyAmount))];

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
        => Task.FromResult(_products.Count == 0 ? Currencies.Idr : _products[0].MonthlyPrice.Currency);
}
