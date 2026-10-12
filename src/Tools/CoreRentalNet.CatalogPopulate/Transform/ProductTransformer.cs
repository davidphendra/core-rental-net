using System.Globalization;
using CoreRentalNet.CatalogPopulate.Catalog;
using CoreRentalNet.CatalogPopulate.Compact;
using CoreRentalNet.CatalogPopulate.Metadata;
using CoreRentalNet.CatalogPopulate.Selection;

namespace CoreRentalNet.CatalogPopulate.Transform;

/// <summary>Builds the products.json row for one product from the compact input and the generated metadata.</summary>
/// <remarks>
/// The split is the catalogue's own: everything measurable is copied or computed here, and everything
/// semantic or normalised comes from <see cref="GeneratedProductMetadata"/>. No attribute is ever retyped by
/// a model but the generated seven.
/// </remarks>
internal sealed class ProductTransformer
{
    /// <summary>The row for one product.</summary>
    public ProductRecord Transform(
        CompactProduct product,
        SourceCategory category,
        GeneratedProductMetadata generated)
    {
        var meta = product.Meta;
        var sku = ProductIdentifier.Sku(category.Prefix, product.ParentAsin);
        var pricePerMonth = PriceRule.MonthlyIdr(meta.Price ?? 0);

        return new ProductRecord
        {
            SkuNo = sku,
            Name = meta.Title,
            Category = category.Category,
            PricePerMonth = pricePerMonth,
            Currency = "IDR",
            Description = string.Join(" ", meta.Description),
            SubCategory = category.SubCategory,
            Image = meta.Image,
            Badge = FeaturedProducts.BadgeFor(sku),
            Metadata = new ProductMetadataRecord
            {
                Tags = [.. generated.Tags],
                Attributes = Attributes(product, category, pricePerMonth, generated),
                BestFor = [.. generated.BestFor],
                NotFor = [.. generated.NotFor],
            },
        };
    }

    private static Dictionary<string, string> Attributes(
        CompactProduct product,
        SourceCategory category,
        int pricePerMonth,
        GeneratedProductMetadata generated)
    {
        var meta = product.Meta;
        var details = meta.Details;

        var attributes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [AttributeNames.Brand] = Or(meta.Store),
            [AttributeNames.Rating] = meta.AverageRating is { } rating ? rating.ToString("0.0", CultureInfo.InvariantCulture) : "0.0",
            [AttributeNames.RatingCount] = (meta.RatingNumber ?? 0).ToString(CultureInfo.InvariantCulture),
            [AttributeNames.PriceUsd] = (meta.Price ?? 0).ToString("0.##", CultureInfo.InvariantCulture),
            [AttributeNames.PriceIdrPerMonth] = pricePerMonth.ToString(CultureInfo.InvariantCulture),
            [AttributeNames.Dimensions] = DetailReader.Text(details, "Product Dimensions"),
            [AttributeNames.Material] = Or(generated.Material),
            [AttributeNames.Color] = DetailReader.Text(details, "Color"),
            [AttributeNames.Weight] = DetailReader.Text(details, "Item Weight"),
            [AttributeNames.CategoryPath] = string.Join(" > ", meta.Categories),
        };

        attributes[category.Category == "desk" ? AttributeNames.DeskType : AttributeNames.Type] = generated.Type;
        attributes[AttributeNames.AssemblyRequired] = Or(generated.AssemblyRequired);
        attributes[AttributeNames.ReviewCount] = product.ReviewCount.ToString(CultureInfo.InvariantCulture);
        attributes[AttributeNames.ReviewSummary] = Or(generated.ReviewSummary);

        return attributes;
    }

    private static string Or(string? value)
        => string.IsNullOrWhiteSpace(value) ? AttributeNames.Unspecified : value.Trim();
}
