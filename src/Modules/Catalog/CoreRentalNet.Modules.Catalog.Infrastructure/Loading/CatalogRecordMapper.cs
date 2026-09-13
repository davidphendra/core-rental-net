using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Catalog.Domain;

namespace CoreRentalNet.Modules.Catalog.Infrastructure.Loading;

/// <summary>
/// Turns one record from a catalog file into a product, refusing anything the domain would not accept
/// and naming the record it refused.
/// </summary>
/// <remarks>
/// Pure: it reads no file and touches no disk. Every rule about what a record has to say lives here,
/// which is what lets those rules be tested from records alone. The loader's remaining job is to read,
/// map and check for duplicates.
/// </remarks>
internal sealed class CatalogRecordMapper(IProductImages images)
{
    public Product Map(CatalogFileRecord record, string source)
    {
        ArgumentNullException.ThrowIfNull(record);

        var skuCode = record.SkuNo ?? "(missing skuNo)";

        if (!Sku.TryParse(record.SkuNo, out var sku))
        {
            throw new CatalogLoadException($"Catalog file '{source}': '{skuCode}' has an invalid skuNo.");
        }

        var category = ParseCategory(record.Category, source, skuCode);
        var subCategory = ParseSubCategory(record.SubCategory, source, skuCode);
        var badge = ParseBadge(record.Badge, source, skuCode);
        var imagePath = record.Image;

        if (string.IsNullOrWhiteSpace(imagePath))
        {
            throw new CatalogLoadException($"Catalog file '{source}': '{skuCode}' has no image path.");
        }

        var image = images.Resolve(sku, imagePath);

        try
        {
            return new Product(
                sku,
                record.Name ?? string.Empty,
                category,
                subCategory,
                Money.Idr(record.PricePerMonth),
                record.Description ?? string.Empty,
                image.Path,
                badge,
                image.Available);
        }
        catch (DomainRuleViolationException exception)
        {
            throw new CatalogLoadException(
                $"Catalog file '{source}': '{skuCode}' is invalid. {exception.Message}",
                exception);
        }
    }

    private static ProductCategory ParseCategory(string? value, string source, string skuCode)
        => value?.Trim().ToLowerInvariant() switch
        {
            "chair" => ProductCategory.Chair,
            "desk" => ProductCategory.Desk,
            "accessory" => ProductCategory.Accessory,
            _ => throw new CatalogLoadException(
                $"Catalog file '{source}': '{skuCode}' has the unknown category '{value}'. Expected chair, desk or accessory."),
        };

    private static ProductSubCategory? ParseSubCategory(string? value, string source, string skuCode)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "beanbag" => ProductSubCategory.Beanbag,
            "coffee" => ProductSubCategory.Coffee,
            "lamp" => ProductSubCategory.Lamp,
            "monitor" => ProductSubCategory.Monitor,
            "plant" => ProductSubCategory.Plant,
            _ => throw new CatalogLoadException(
                $"Catalog file '{source}': '{skuCode}' has the unknown subcategory '{value}'."),
        };
    }

    private static ProductBadge? ParseBadge(string? value, string source, string skuCode)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "popular" => ProductBadge.Popular,
            _ => throw new CatalogLoadException(
                $"Catalog file '{source}': '{skuCode}' has the unknown badge '{value}'."),
        };
    }
}
