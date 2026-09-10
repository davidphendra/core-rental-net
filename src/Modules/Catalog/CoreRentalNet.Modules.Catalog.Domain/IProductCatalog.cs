namespace CoreRentalNet.Modules.Catalog.Domain;

/// <summary>
/// The read port over the catalog. There is deliberately no write side: the catalog is
/// an immutable snapshot loaded from a file at start-up.
/// </summary>
public interface IProductCatalog
{
    IReadOnlyList<Product> All { get; }

    /// <summary>The distinct subcategories actually present, used to derive the zone list.</summary>
    IReadOnlyList<ProductSubCategory> SubCategories { get; }

    Product? Find(Sku sku);

    IReadOnlyList<Product> ByCategory(ProductCategory category);

    IReadOnlyList<Product> BySubCategory(ProductSubCategory subCategory);

    IReadOnlyList<Product> Featured();
}
