using System.Text.Json;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Catalog.Domain;

namespace CoreRentalNet.Modules.Catalog.Infrastructure.Loading;

/// <summary>
/// Reads products.json into an immutable catalog. Fails loudly and specifically: every
/// message names the file and, where relevant, the offending SKU and field.
/// </summary>
public static class CatalogLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    public static ProductCatalogSnapshot LoadFromFile(string path, string? webRootPath = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new CatalogLoadException("No catalog file path was configured.");
        }

        if (!File.Exists(path))
        {
            throw new CatalogLoadException($"Catalog file not found: '{path}'.");
        }

        string json;

        try
        {
            json = File.ReadAllText(path);
        }
        catch (IOException exception)
        {
            throw new CatalogLoadException($"Catalog file could not be read: '{path}'.", exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new CatalogLoadException($"Catalog file could not be read: '{path}'.", exception);
        }

        List<CatalogFileRecord>? records;

        try
        {
            records = JsonSerializer.Deserialize<List<CatalogFileRecord>>(json, Options);
        }
        catch (JsonException exception)
        {
            throw new CatalogLoadException(
                $"Catalog file is not valid JSON: '{path}'. {exception.Message}",
                exception);
        }

        if (records is null || records.Count == 0)
        {
            throw new CatalogLoadException($"Catalog file contains no products: '{path}'.");
        }

        var products = new List<Product>(records.Count);

        foreach (var record in records)
        {
            products.Add(Map(record, path, webRootPath));
        }

        var duplicates = products
            .GroupBy(product => product.Sku.Value, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(sku => sku, StringComparer.Ordinal)
            .ToArray();

        if (duplicates.Length > 0)
        {
            throw new CatalogLoadException(
                $"Catalog file '{path}' contains duplicate SKUs: {string.Join(", ", duplicates)}.");
        }

        return new ProductCatalogSnapshot(products);
    }

    private static Product Map(CatalogFileRecord record, string path, string? webRootPath)
    {
        var skuCode = record.SkuNo ?? "(missing skuNo)";

        if (!Sku.TryParse(record.SkuNo, out var sku))
        {
            throw new CatalogLoadException($"Catalog file '{path}': '{skuCode}' has an invalid skuNo.");
        }

        var category = ParseCategory(record.Category, path, skuCode);
        var subCategory = ParseSubCategory(record.SubCategory, path, skuCode);
        var badge = ParseBadge(record.Badge, path, skuCode);
        var imagePath = record.Image;

        if (string.IsNullOrWhiteSpace(imagePath))
        {
            throw new CatalogLoadException($"Catalog file '{path}': '{skuCode}' has no image path.");
        }

        imagePath = PreferVendoredImage(imagePath, sku, webRootPath);

        try
        {
            return new Product(
                sku,
                record.Name ?? string.Empty,
                category,
                subCategory,
                Money.Idr(record.PricePerMonth),
                record.Description ?? string.Empty,
                imagePath,
                badge,
                IsImageAvailable(imagePath, webRootPath));
        }
        catch (DomainRuleViolationException exception)
        {
            throw new CatalogLoadException($"Catalog file '{path}': '{skuCode}' is invalid. {exception.Message}", exception);
        }
    }

    private static ProductCategory ParseCategory(string? value, string path, string skuCode)
        => value?.Trim().ToLowerInvariant() switch
        {
            "chair" => ProductCategory.Chair,
            "desk" => ProductCategory.Desk,
            "accessory" => ProductCategory.Accessory,
            _ => throw new CatalogLoadException(
                $"Catalog file '{path}': '{skuCode}' has the unknown category '{value}'. Expected chair, desk or accessory."),
        };

    private static ProductSubCategory? ParseSubCategory(string? value, string path, string skuCode)
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
                $"Catalog file '{path}': '{skuCode}' has the unknown subcategory '{value}'."),
        };
    }

    private static ProductBadge? ParseBadge(string? value, string path, string skuCode)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "popular" => ProductBadge.Popular,
            _ => throw new CatalogLoadException(
                $"Catalog file '{path}': '{skuCode}' has the unknown badge '{value}'."),
        };
    }

    /// <summary>
    /// A remote image is replaced by a local copy when one has been vendored for this SKU, so
    /// the running app never depends on a third-party host.
    /// </summary>
    private static string PreferVendoredImage(string imagePath, Sku sku, string? webRootPath)
    {
        if (!imagePath.StartsWith("http", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(webRootPath))
        {
            return imagePath;
        }

        var directory = Path.Combine(webRootPath, VendoredImageDirectory.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        if (!Directory.Exists(directory))
        {
            return imagePath;
        }

        foreach (var candidate in Directory.GetFiles(directory))
        {
            if (string.Equals(Path.GetFileNameWithoutExtension(candidate), sku.Value, StringComparison.OrdinalIgnoreCase))
            {
                return $"{VendoredImageDirectory}/{Path.GetFileName(candidate)}";
            }
        }

        return imagePath;
    }

    /// <summary>Where locally vendored product images live, relative to the web root.</summary>
    public const string VendoredImageDirectory = "/images/vendored";

    /// <summary>
    /// A remote image is assumed to exist; a local path is checked against the web root when
    /// one is supplied. Anything unverifiable is reported as unavailable so the UI renders a
    /// placeholder rather than a broken image.
    /// </summary>
    private static bool IsImageAvailable(string imagePath, string? webRootPath)
    {
        if (imagePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || imagePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            return false;
        }

        var relativePath = imagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        return File.Exists(Path.Combine(webRootPath, relativePath));
    }
}
