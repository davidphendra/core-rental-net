using CoreRentalNet.Modules.Catalog.Domain;

namespace CoreRentalNet.Modules.Catalog.Infrastructure.Loading;

/// <summary>
/// The immutable in-memory catalog. Built once at start-up and never mutated, which is why
/// no write method exists on it.
/// </summary>
public sealed class ProductCatalogSnapshot : IProductCatalog
{
    private readonly Product[] products;
    private readonly ProductSubCategory[] subCategories;

    public ProductCatalogSnapshot(IEnumerable<Product> products)
    {
        ArgumentNullException.ThrowIfNull(products);

        this.products = products.ToArray();

        subCategories = this.products
            .Where(product => product.SubCategory.HasValue)
            .Select(product => product.SubCategory.GetValueOrDefault())
            .Distinct()
            .OrderBy(subCategory => subCategory)
            .ToArray();
    }

    public IReadOnlyList<Product> All => products;

    public IReadOnlyList<ProductSubCategory> SubCategories => subCategories;

    public Product? Find(Sku sku)
    {
        ArgumentNullException.ThrowIfNull(sku);

        foreach (var product in products)
        {
            if (product.Sku == sku)
            {
                return product;
            }
        }

        return null;
    }

    public IReadOnlyList<Product> ByCategory(ProductCategory category)
        => products.Where(product => product.Category == category).ToArray();

    public IReadOnlyList<Product> BySubCategory(ProductSubCategory subCategory)
        => products.Where(product => product.SubCategory == subCategory).ToArray();

    public IReadOnlyList<Product> Featured()
        => products.Where(product => product.IsFeatured).ToArray();
}
