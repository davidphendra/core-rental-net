using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Catalog.Domain;
using CoreRentalNet.Modules.Catalog.Infrastructure.Contracts;
using CoreRentalNet.Modules.Catalog.Infrastructure.ProductImage;

namespace CoreRentalNet.Modules.Catalog.Infrastructure.Loader;

/// <summary>
/// Turns the rows of products.json into domain records. Fails loudly and specifically: every message
/// names the file and, once the row is known, the SKU and the field that is wrong.
/// </summary>
/// <remarks>
/// Reading the file is <see cref="ProductFile"/>'s job; what is left here is what a row has to mean.
/// Field text is matched without regard to letter case, because the file writes "chair" while the enum
/// member is <c>Chair</c>. A subcategory is optional, because chairs and desks do not carry one, and
/// the image's path and availability come from the resolver rather than from the file text.
/// </remarks>
internal static class ProductLoader
{
    /// <summary>Reads the file and turns every row into a product, or throws naming what is wrong.</summary>
    public static IReadOnlyList<Product> Read(string path, IProductImage images)
    {
        ArgumentNullException.ThrowIfNull(images);

        var records = ProductFile.ReadRows(path);

        if (records.Count == 0)
        {
            throw new ProductLoadException($"Catalog file contains no products: '{path}'.");
        }

        var products = records.Select(record => Map(record, path, images)).ToList();

        EnsureNoDuplicateSkus(products, path);

        return products;
    }

    private static Product Map(ProductJsonRecord record, string path, IProductImage images)
    {
        var sku = RequireSku(record.SkuNo, path);
        var category = ParseCategory(record.Category, sku, path);
        var subCategory = ParseSubCategory(record.SubCategory, sku, path);

        EnsureShape(category, subCategory, sku, path);

        var resolved = images.Resolve(sku, Require(record.Image, "image", sku, path));

        return new Product(
            sku,
            Require(record.Name, "name", sku, path),
            category,
            subCategory,
            ParsePrice(record, sku, path),
            Require(record.Description, "description", sku, path),
            resolved.Path,
            ParseBadge(record.Badge, sku, path),
            resolved.Available);
    }

    private static ProductCategory ParseCategory(string? value, string sku, string path)
        => Enum.TryParse<ProductCategory>(value, ignoreCase: true, out var category) && Enum.IsDefined(category)
            ? category
            : throw new ProductLoadException($"Catalog entry '{sku}' has an unknown category '{value}': '{path}'.");

    private static ProductSubCategory? ParseSubCategory(string? value, string sku, string path)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : Enum.TryParse<ProductSubCategory>(value, ignoreCase: true, out var subCategory) && Enum.IsDefined(subCategory)
                ? subCategory
                : throw new ProductLoadException($"Catalog entry '{sku}' has an unknown subcategory '{value}': '{path}'.");

    private static bool ParseBadge(string? value, string sku, string path)
        => string.IsNullOrWhiteSpace(value)
            ? false
            : Enum.TryParse<ProductBadge>(value, ignoreCase: true, out var badge) && Enum.IsDefined(badge)
                ? true
                : throw new ProductLoadException($"Catalog entry '{sku}' has an unknown badge '{value}': '{path}'.");

    private static Money ParsePrice(ProductJsonRecord record, string sku, string path)
    {
        if (record.PricePerMonth < 0m)
        {
            throw new ProductLoadException($"Catalog entry '{sku}' has a price that cannot be negative: '{path}'.");
        }

        var currency = ParseCurrency(record.Currency, sku, path);

        // A price in a currency this application cannot charge would otherwise reach the checkout and
        // be summed with IDR, where the money rules refuse it with a message that names neither the
        // entry nor the file. Refusing it here is the same rule, said where the file is still in hand.
        if (currency != Currencies.Idr)
        {
            throw new ProductLoadException(
                $"Catalog entry '{sku}' is priced in {currency}, but this application settles in {Currencies.Idr}: '{path}'.");
        }

        return new Money(record.PricePerMonth, currency);
    }

    /// <summary>The currency the price is stated in. Absent means the settlement currency.</summary>
    private static string ParseCurrency(string? value, string sku, string path)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Currencies.Idr;
        }

        var code = value.Trim().ToUpperInvariant();

        return Currencies.IsWellFormed(code)
            ? code
            : throw new ProductLoadException($"Catalog entry '{sku}' has an unknown currency '{value}': '{path}'.");
    }

    /// <summary>An accessory needs a subcategory, and a chair or a desk must not carry one.</summary>
    private static void EnsureShape(ProductCategory category, ProductSubCategory? subCategory, string sku, string path)
    {
        if (category == ProductCategory.Accessory && subCategory is null)
        {
            throw new ProductLoadException($"Catalog entry '{sku}' is an accessory and requires a subcategory: '{path}'.");
        }

        if (category != ProductCategory.Accessory && subCategory is not null)
        {
            throw new ProductLoadException(
                $"Catalog entry '{sku}' is a {category} and cannot have the subcategory '{subCategory}': '{path}'.");
        }
    }

    private static string RequireSku(string? value, string path)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ProductLoadException($"Catalog entry has no usable skuNo: '{path}'.")
            : value.Trim();

    private static string Require(string? value, string field, string sku, string path)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ProductLoadException($"Catalog entry '{sku}' has no {field}: '{path}'.")
            : value.Trim();

    private static void EnsureNoDuplicateSkus(IReadOnlyList<Product> products, string path)
    {
        var duplicates = products
            .GroupBy(product => product.Sku, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(sku => sku, StringComparer.Ordinal)
            .ToArray();

        if (duplicates.Length > 0)
        {
            throw new ProductLoadException(
                $"Catalog file contains duplicate SKUs: {string.Join(", ", duplicates)}: '{path}'.");
        }
    }
}
