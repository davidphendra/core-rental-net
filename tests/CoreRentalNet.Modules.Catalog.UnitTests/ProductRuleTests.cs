using AwesomeAssertions;
using CoreRentalNet.Modules.Catalog.Infrastructure.Contracts;
using CoreRentalNet.Modules.Catalog.Infrastructure.Loader;
using CoreRentalNet.Modules.Catalog.Infrastructure.ProductImage;
using Xunit;

namespace CoreRentalNet.Modules.Catalog.UnitTests;

/// <summary>
/// The rules a catalog row has to satisfy, asserted through the loader that enforces them: the record
/// itself carries no validation, so the loader is what refuses a row the catalogue would not accept.
/// </summary>
public sealed class ProductRuleTests
{
    private static readonly IProductImage Images = new StubProductImage(new ResolvedProductImage("/images/x.svg", true));

    [Fact]
    public void An_accessory_requires_a_subcategory()
        => Refuses(Row(sku: "MON0001", category: "accessory"), "requires a subcategory");

    [Fact]
    public void A_chair_cannot_carry_a_subcategory()
        => Refuses(Row(sku: "CHA0001", category: "chair", subCategory: "monitor"), "cannot have the subcategory");

    [Fact]
    public void A_product_requires_a_name()
        => Refuses(Row(name: "   "), "no name");

    [Fact]
    public void A_product_requires_a_description()
        => Refuses(Row(description: "  "), "no description");

    [Fact]
    public void A_negative_price_is_refused()
        => Refuses(Row(price: -1m), "cannot be negative");

    [Fact]
    public void An_unknown_category_is_refused_and_named()
        => Refuses(Row(category: "sofa"), "unknown category 'sofa'");

    [Fact]
    public void An_unknown_subcategory_is_refused_and_named()
        => Refuses(Row(sku: "MON0001", category: "accessory", subCategory: "hammock"), "unknown subcategory 'hammock'");

    [Fact]
    public void An_unknown_badge_is_refused_and_named()
        => Refuses(Row(badge: "shiny"), "unknown badge 'shiny'");

    [Fact]
    public void A_row_without_a_usable_sku_is_refused()
        => Refuses(Row(sku: "   "), "no usable skuNo");

    [Fact]
    public void A_row_without_an_image_path_is_refused()
        => Refuses(Row(image: "   "), "no image");

    [Fact]
    public void Duplicate_skus_are_refused_whatever_their_case()
    {
        using var file = new TemporaryCatalogFile($"[{Row(sku: "AAA0001", category: "chair")},{Row(sku: "aaa0001", category: "chair")}]");

        var action = () => _ = ProductLoader.Read(file.Path, Images);

        action.Should().Throw<ProductLoadException>().WithMessage("*duplicate*AAA0001*");
    }

    private static void Refuses(string row, string expected)
    {
        using var file = new TemporaryCatalogFile($"[{row}]");

        var action = () => _ = ProductLoader.Read(file.Path, Images);

        action.Should().Throw<ProductLoadException>().WithMessage($"*{expected}*");
    }

    private static string Row(
        string sku = "DSK0001",
        string name = "A desk",
        string category = "desk",
        string? subCategory = null,
        decimal price = 800000m,
        string description = "A desk.",
        string image = "/images/desk.svg",
        string? badge = null)
    {
        var fields = new List<string>
        {
            $"\"skuNo\": {Text(sku)}",
            $"\"name\": {Text(name)}",
            $"\"category\": {Text(category)}",
            $"\"pricePerMonth\": {price}",
            $"\"description\": {Text(description)}",
            $"\"image\": {Text(image)}",
        };

        if (subCategory is not null)
        {
            fields.Add($"\"subCategory\": {Text(subCategory)}");
        }

        if (badge is not null)
        {
            fields.Add($"\"badge\": {Text(badge)}");
        }

        return "{" + string.Join(", ", fields) + "}";
    }

    private static string Text(string? value) => value is null ? "null" : $"\"{value}\"";
}
