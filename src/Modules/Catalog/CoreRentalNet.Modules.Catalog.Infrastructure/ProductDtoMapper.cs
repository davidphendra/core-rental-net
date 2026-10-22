using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Domain;

namespace CoreRentalNet.Modules.Catalog.Infrastructure;

/// <summary>Presents a domain record as the view the read port publishes.</summary>
/// <remarks>
/// The two vocabularies are deliberately separate, so this mapper is the only place they meet:
/// <c>Catalog.Domain.ProductCategory</c> stays inside this module, and <c>CatalogCategory</c> is what
/// callers see. A category added to one vocabulary without the other fails to compile here.
/// </remarks>
public static class ProductDtoMapper
{
    /// <summary>The DTO a caller reads, translated from the record the loader built.</summary>
    public static ProductDto ToDto(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        return new ProductDto(
            product.Sku,
            product.Name,
            Category(product.Category),
            product.SubCategory is { } subCategory ? ToCatalogSubCategory(subCategory) : null,
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
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown catalogService category."),
        };

    /// <summary>The published vocabulary for one stored subcategory.</summary>
    public static CatalogSubCategory ToCatalogSubCategory(ProductSubCategory subCategory)
        => subCategory switch
        {
            ProductSubCategory.Beanbag => CatalogSubCategory.Beanbag,
            ProductSubCategory.Coffee => CatalogSubCategory.Coffee,
            ProductSubCategory.Lamp => CatalogSubCategory.Lamp,
            ProductSubCategory.Monitor => CatalogSubCategory.Monitor,
            ProductSubCategory.Plant => CatalogSubCategory.Plant,
            _ => throw new ArgumentOutOfRangeException(nameof(subCategory), subCategory, "Unknown catalogService subcategory."),
        };

    /// <summary>The stored vocabulary for one published subcategory.</summary>
    public static ProductSubCategory ToProductSubCategory(CatalogSubCategory subCategory)
        => subCategory switch
        {
            CatalogSubCategory.Beanbag => ProductSubCategory.Beanbag,
            CatalogSubCategory.Coffee => ProductSubCategory.Coffee,
            CatalogSubCategory.Lamp => ProductSubCategory.Lamp,
            CatalogSubCategory.Monitor => ProductSubCategory.Monitor,
            CatalogSubCategory.Plant => ProductSubCategory.Plant,
            _ => throw new ArgumentOutOfRangeException(nameof(subCategory), subCategory, "Unknown application subcategory."),
        };
}
