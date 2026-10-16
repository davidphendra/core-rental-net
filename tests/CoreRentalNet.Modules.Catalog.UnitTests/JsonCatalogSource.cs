using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Infrastructure;
using CoreRentalNet.Modules.Catalog.Infrastructure.Loader;
using ProductImageResolver = CoreRentalNet.Modules.Catalog.Infrastructure.ProductImage.ProductImage;

namespace CoreRentalNet.Modules.Catalog.UnitTests;

/// <summary>
/// A test-only source over a catalogue JSON file, for the loader and query tests.
/// </summary>
/// <remarks>
/// The application reads the store now, so this stands in for the file those tests still use. It reuses the
/// same loader the seed was generated from, so what it returns is what the migration carries.
/// </remarks>
internal sealed class JsonCatalogSource : IProductCatalogService
{
    private readonly ProductDto[] _products;

    public JsonCatalogSource(string path, string? webRootPath)
    {
        _products = [.. ProductLoader.Read(path, new ProductImageResolver(webRootPath)).Select(ProductDtoMapper.ToDto)];
    }

    public IReadOnlyList<ProductDto> All => _products;

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
        => Task.FromResult(_products.Length == 0 ? Currencies.Idr : _products[0].MonthlyPrice.Currency);
}
