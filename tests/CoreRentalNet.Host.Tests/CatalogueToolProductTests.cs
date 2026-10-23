using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Host.Configs;
using CoreRentalNet.Host.Mcp;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>The MCP read model keeps every product field and bounds only the description.</summary>
public sealed class CatalogueToolProductTests
{
    private const string LongDescription =
        "A long catalogue description that must be shortened before it reaches a model, while every other "
        + "published product property survives the projection unchanged.";

    [Fact]
    public void Every_product_field_survives_the_projection()
    {
        var source = Product(LongDescription);

        var product = CatalogueToolProduct.From(source);

        product.Sku.Should().Be(source.Sku);
        product.Name.Should().Be(source.Name);
        product.Category.Should().Be(source.Category);
        product.SubCategory.Should().Be(source.SubCategory);
        product.MonthlyPrice.Should().Be(source.MonthlyPrice);
        product.ImagePath.Should().Be(source.ImagePath);
        product.ImageAvailable.Should().Be(source.ImageAvailable);
        product.IsFeatured.Should().Be(source.IsFeatured);
    }

    [Fact]
    public void The_description_is_bounded_without_mutating_the_source()
    {
        var source = Product(LongDescription);

        var product = CatalogueToolProduct.From(source);

        product.Description.Length.Should().Be(CatalogToolLimits.MaximumDescriptionCharacterCount);
        source.Description.Should().Be(LongDescription);
    }

    [Fact]
    public void A_short_description_is_preserved()
    {
        const string shortDescription = "A compact desk.";

        CatalogueToolProduct.From(Product(shortDescription)).Description.Should().Be(shortDescription);
    }

    private static ProductDto Product(string description)
        => new(
            "DSKB0B6MCK87",
            "HON Mod Desk Shell, 60 x 30 x 29, Slate Teak",
            CatalogCategory.Desk,
            null,
            new Money(271000m, Currencies.Idr),
            description,
            "/images/vendored/DSKB0B6MCK87.jpg",
            true,
            false);
}
