using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Domain;

namespace CoreRentalNet.Modules.Catalog.Infrastructure;

/// <summary>Presents a domain record as the view the read port publishes.</summary>
/// <remarks>
/// The two vocabularies are deliberately separate, so this mapper is the only place they meet:
/// <c>Catalog.Domain.ProductCategory</c> stays inside this module, and <c>CatalogCategory</c> is what
/// callers see. A category added to one vocabulary without the other fails to compile here.
/// </remarks>
public static class ProductViewMapper
{
    /// <summary>The view a caller reads, translated from the record the loader built.</summary>
    public static ProductView ToView(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        return new ProductView(
            product.Sku,
            product.Name,
            Category(product.Category),
            product.SubCategory is { } subCategory ? SubCategory(subCategory) : null,
            product.MonthlyPrice,
            product.Description,
            product.ImagePath,
            product.ImageAvailable,
            product.IsFeatured);
    }

    private static CatalogCategory Category(ProductCategory category)
        => category switch
        {
            ProductCategory.Chair => CatalogCategory.Chair,
            ProductCategory.Desk => CatalogCategory.Desk,
            ProductCategory.Accessory => CatalogCategory.Accessory,
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown catalog category."),
        };

    private static CatalogSubCategory SubCategory(ProductSubCategory subCategory)
        => subCategory switch
        {
            ProductSubCategory.Beanbag => CatalogSubCategory.Beanbag,
            ProductSubCategory.Coffee => CatalogSubCategory.Coffee,
            ProductSubCategory.Lamp => CatalogSubCategory.Lamp,
            ProductSubCategory.Monitor => CatalogSubCategory.Monitor,
            ProductSubCategory.Plant => CatalogSubCategory.Plant,
            _ => throw new ArgumentOutOfRangeException(nameof(subCategory), subCategory, "Unknown catalog subcategory."),
        };
}
