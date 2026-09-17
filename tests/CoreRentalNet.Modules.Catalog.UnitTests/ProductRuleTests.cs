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
    public void An_unknown_currency_is_refused_and_named()
        => Refuses(Row(currency: "RUPIAH"), "unknown currency 'RUPIAH'");

    [Fact]
    public void A_price_in_another_currency_is_refused()
        => Refuses(Row(currency: "USD"), "priced in USD");

    [Fact]
    public void Duplicate_skus_are_refused_whatever_their_case()
    {
        using var file = new TemporaryCatalogFile($"[{Row(sku: "AAA0001", category: "chair")},{Row(sku: "aaa0001", category: "chair")}]");

        var action = () => _ = ProductLoader.Read(file.Path, Images);

        action.Should().Throw<ProductLoadException>().WithMessage("*duplicate*AAA0001*");
    }

    [Fact]
    public void A_row_without_metadata_is_refused()
        => Refuses(Row(metadata: null), "has no metadata");

    [Fact]
    public void Metadata_with_an_unknown_key_is_refused_and_named()
        => Refuses(Row(metadata: """{ "tags": ["desk"], "useCases": ["x"] }"""), "unknown metadata key 'useCases'");

    [Fact]
    public void Metadata_with_an_attribute_that_is_not_text_is_refused_and_named()
        => Refuses(Row(metadata: """{ "tags": ["desk"], "attributes": { "load": 120 } }"""), "attribute 'load' that is not text");

    [Fact]
    public void Metadata_with_a_blank_tag_is_refused()
        => Refuses(Row(metadata: """{ "tags": ["desk", "  "] }"""), "blank tag");

    [Fact]
    public void Metadata_with_no_tags_is_refused()
        => Refuses(Row(metadata: """{ "tags": [], "attributes": { "type": "task" } }"""), "no tags");

    [Fact]
    public void Metadata_is_kept_on_the_product()
    {
        const string metadata = """{ "tags": ["desk", "standing"], "attributes": { "type": "sit-stand" } }""";

        using var file = new TemporaryCatalogFile($"[{Row(metadata: metadata)}]");

        var product = ProductLoader.Read(file.Path, Images).Single();

        product.Metadata.Tags.Should().BeEquivalentTo(["desk", "standing"]);
        product.Metadata.Attributes["type"].Should().Be("sit-stand");
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
        string? currency = null,
        string description = "A desk.",
        string image = "/images/desk.svg",
        string? badge = null,
        string? metadata = "{\"tags\":[\"desk\",\"task\"],\"attributes\":{\"type\":\"task\"},\"bestFor\":[\"focused work\"],\"notFor\":[\"outdoor use\"]}")
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

        if (metadata is not null)
        {
            fields.Add($"\"metadata\": {metadata}");
        }

        if (subCategory is not null)
        {
            fields.Add($"\"subCategory\": {Text(subCategory)}");
        }

        if (currency is not null)
        {
            fields.Add($"\"currency\": {Text(currency)}");
        }

        if (badge is not null)
        {
            fields.Add($"\"badge\": {Text(badge)}");
        }

        return "{" + string.Join(", ", fields) + "}";
    }

    private static string Text(string? value) => value is null ? "null" : $"\"{value}\"";
}
