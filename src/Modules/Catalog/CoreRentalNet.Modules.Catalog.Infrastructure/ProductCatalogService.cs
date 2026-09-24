using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Infrastructure.Loader;
using ProductImageResolver = CoreRentalNet.Modules.Catalog.Infrastructure.ProductImage.ProductImage;

namespace CoreRentalNet.Modules.Catalog.Infrastructure;

/// <summary>
/// The catalogue's adapter: an immutable, in-memory projection of the domain records loaded at
/// start-up. It implements the read port and never mutates, which is why no write method exists on it.
/// </summary>
/// <remarks>
/// The JSON file builds it. A database-backed adapter would replace this type, and would also have to
/// make the port's operations asynchronous, because a database read is not immediate. The web root is
/// what lets each product's image be checked once, at load, rather than on every page.
/// </remarks>
public sealed class ProductCatalogService : IProductCatalogService
{
    private readonly ProductView[] _views;

    public ProductCatalogService(string jsonPath, string? webRootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonPath);

        var products = ProductLoader.Read(jsonPath, new ProductImageResolver(webRootPath));

        _views = products.Select(ProductViewMapper.ToView).ToArray();
    }

    /// <inheritdoc />
    public IReadOnlyList<ProductView> All => _views;

    /// <inheritdoc />
    public ProductView? Find(string sku)
    {
        ArgumentNullException.ThrowIfNull(sku);

        return _views.FirstOrDefault(view => string.Equals(view.Sku, sku, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public IReadOnlyList<ProductView> ByCategory(CatalogCategory category)
        => _views.Where(view => view.Category == category).ToArray();

    /// <inheritdoc />
    public IReadOnlyList<ProductView> BySubCategory(CatalogSubCategory subCategory)
        => _views.Where(view => view.SubCategory == subCategory).ToArray();

    /// <inheritdoc />
    public IReadOnlyList<ProductView> Featured()
        => _views.Where(view => view.IsFeatured).ToArray();

    /// <inheritdoc />
    public IReadOnlyList<ProductView> Search(CatalogCategory? category, CatalogSubCategory? subCategory)
        =>
        [
            .. _views.Where(view => (category is null || view.Category == category)
                                && (subCategory is null || view.SubCategory == subCategory)),
        ];
}
