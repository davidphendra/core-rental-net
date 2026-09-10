using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Domain;

namespace CoreRentalNet.Modules.Catalog.Application.Catalog;

/// <summary>Translates between the Domain vocabulary and the contract/DTO vocabulary.</summary>
public static class CatalogMapping
{
    public static CatalogCategory ToContract(this ProductCategory category) => category switch
    {
        ProductCategory.Chair => CatalogCategory.Chair,
        ProductCategory.Desk => CatalogCategory.Desk,
        ProductCategory.Accessory => CatalogCategory.Accessory,
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown product category."),
    };

    public static ProductCategory ToDomain(this CatalogCategory category) => category switch
    {
        CatalogCategory.Chair => ProductCategory.Chair,
        CatalogCategory.Desk => ProductCategory.Desk,
        CatalogCategory.Accessory => ProductCategory.Accessory,
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown catalog category."),
    };

    public static CatalogSubCategory ToContract(this ProductSubCategory subCategory) => subCategory switch
    {
        ProductSubCategory.Beanbag => CatalogSubCategory.Beanbag,
        ProductSubCategory.Coffee => CatalogSubCategory.Coffee,
        ProductSubCategory.Lamp => CatalogSubCategory.Lamp,
        ProductSubCategory.Monitor => CatalogSubCategory.Monitor,
        ProductSubCategory.Plant => CatalogSubCategory.Plant,
        _ => throw new ArgumentOutOfRangeException(nameof(subCategory), subCategory, "Unknown product subcategory."),
    };

    public static CatalogSubCategory? ToContract(this ProductSubCategory? subCategory)
        => subCategory is null ? null : ToContract(subCategory.Value);

    public static ProductSubCategory ToDomain(this CatalogSubCategory subCategory) => subCategory switch
    {
        CatalogSubCategory.Beanbag => ProductSubCategory.Beanbag,
        CatalogSubCategory.Coffee => ProductSubCategory.Coffee,
        CatalogSubCategory.Lamp => ProductSubCategory.Lamp,
        CatalogSubCategory.Monitor => ProductSubCategory.Monitor,
        CatalogSubCategory.Plant => ProductSubCategory.Plant,
        _ => throw new ArgumentOutOfRangeException(nameof(subCategory), subCategory, "Unknown catalog subcategory."),
    };

    public static ProductListItem ToListItem(this Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        return new ProductListItem(
            product.Sku.Value,
            product.Name,
            product.Category.ToContract(),
            product.SubCategory.ToContract(),
            product.MonthlyPrice,
            product.Description,
            product.ImagePath,
            product.ImageAvailable,
            product.IsFeatured);
    }

    public static ProductPriceView ToPriceView(this Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        return new ProductPriceView(
            product.Sku.Value,
            product.Name,
            product.Category.ToContract(),
            product.SubCategory.ToContract(),
            product.MonthlyPrice);
    }
}
