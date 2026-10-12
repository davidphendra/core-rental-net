using Xunit;
using System.Text.Json;
using AwesomeAssertions;
using CoreRentalNet.CatalogPopulate.Compact;
using CoreRentalNet.CatalogPopulate.Metadata;
using CoreRentalNet.CatalogPopulate.Selection;
using CoreRentalNet.CatalogPopulate.Transform;

namespace CoreRentalNet.CatalogPopulate.Tests;

public class ProductTransformerTests
{
    private static readonly GeneratedProductMetadata Generated = new(
        Tags: ["desk", "standing"],
        BestFor: ["focused work"],
        NotFor: [],
        ReviewSummary: "A sturdy desk.",
        Type: "standing",
        Material: "Engineered Wood",
        AssemblyRequired: "Yes");

    [Fact]
    public void A_desk_row_carries_the_desk_attributes()
    {
        var record = new ProductTransformer().Transform(Product("B09DCYF7ND", "Desk"), ProductCategories.Desk, Generated);

        record.SkuNo.Should().Be("DSKB09DCYF7N");
        record.Category.Should().Be("desk");
        record.SubCategory.Should().BeNull();
        record.Currency.Should().Be("IDR");
        record.PricePerMonth.Should().Be(PriceRule.MonthlyIdr(120.0));
        record.Description.Should().Be("line one line two");
        record.Metadata!.Tags.Should().Equal("desk", "standing");
        record.Metadata.Attributes.Should().ContainKey("desktype").WhoseValue.Should().Be("standing");
        record.Metadata.Attributes.Should().NotContainKey("type");
        record.Metadata.Attributes["categorypath"].Should().Be("Office Products > Office Furniture & Lighting > Desks & Workstations");
        record.Metadata.Attributes["reviewcount"].Should().Be("7");
        record.Metadata.Attributes["reviewsummary"].Should().Be("A sturdy desk.");
    }

    [Fact]
    public void An_accessory_row_carries_subcategory_and_type()
    {
        var record = new ProductTransformer().Transform(Product("B000000001", "Monitor"), ProductCategories.Monitor, Generated);

        record.Category.Should().Be("accessory");
        record.SubCategory.Should().Be("monitor");
        record.Metadata!.Attributes.Should().ContainKey("type").WhoseValue.Should().Be("standing");
        record.Metadata.Attributes.Should().NotContainKey("desktype");
    }

    [Fact]
    public void A_featured_sku_carries_the_badge()
    {
        var record = new ProductTransformer().Transform(Product("B08R35RW3", "Desk"), ProductCategories.Desk, Generated);

        record.SkuNo.Should().Be("DSKB08R35RW3");
        record.Badge.Should().Be("Popular");
    }

    private static CompactProduct Product(string asin, string title)
    {
        var details = new Dictionary<string, JsonElement>
        {
            ["Brand"] = JsonValue.Of("\"Tangkula\""),
            ["Product Dimensions"] = JsonValue.Of("\"30 x 60\""),
            ["Color"] = JsonValue.Of("\"Oak\""),
            ["Item Weight"] = JsonValue.Of("\"40 pounds\""),
        };

        var meta = new CompactMeta(
            asin,
            title,
            ["line one", "line two"],
            ["Office Products", "Office Furniture & Lighting", "Desks & Workstations"],
            details,
            120.0,
            4.5,
            12,
            "Tangkula",
            "https://example.com/image.jpg");

        return new CompactProduct(asin, "Desk", meta, [], 7);
    }
}