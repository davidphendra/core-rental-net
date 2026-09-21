using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Modules.Discovery.UnitTests;

/// <summary>
/// A catalogue a test can hold: whatever products it was given, and nothing else.
/// </summary>
/// <remarks>
/// Hand-written rather than mocked, which is the convention here and the point of it: an interface small
/// enough to write by hand is an interface whose cost a caller can see. The real one reads a file at start-up;
/// a test that needs a bucket lookup, or needs the lookup to be consulted at all, does not want the file.
/// </remarks>
internal sealed class FakeProductCatalog(params ProductView[] products) : IProductCatalog
{
    public IReadOnlyList<ProductView> All => products;

    public ProductView? Find(string sku)
        => products.FirstOrDefault(product => string.Equals(product.Sku, sku, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<ProductView> ByCategory(CatalogCategory category)
        => [.. products.Where(product => product.Category == category)];

    public IReadOnlyList<ProductView> BySubCategory(CatalogSubCategory subCategory)
        => [.. products.Where(product => product.SubCategory == subCategory)];

    public IReadOnlyList<ProductView> Featured()
        => [.. products.Where(product => product.IsFeatured)];

    public IReadOnlyList<ProductView> Search(CatalogCategory? category, CatalogSubCategory? subCategory, string? search)
        => products;
}
