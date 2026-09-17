using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Domain;
using CoreRentalNet.Modules.Catalog.Infrastructure;
using Xunit;

namespace CoreRentalNet.Modules.Catalog.UnitTests;

/// <summary>A product presented as the view the UI and the other modules read.</summary>
public sealed class MappingTests
{
    [Fact] // CAT-54
    public void A_desk_becomes_the_view_the_ui_reads()
    {
        var product = new Product(
            "DSK0001",
            "A desk",
            ProductCategory.Desk,
            null,
            new Money(800000m, Currencies.Idr),
            "A desk.",
            new ProductMetadata(["desk", "standing"], new Dictionary<string, string> { ["type"] = "sit-stand" }, ["focused work"], ["outdoor use"]),
            "/images/desk.svg",
            IsFeatured: true,
            ImageAvailable: true);

        var view = ProductViewMapper.ToView(product);

        view.Sku.Should().Be(product.Sku);
        view.Name.Should().Be(product.Name);
        view.Category.Should().Be(CatalogCategory.Desk);
        view.SubCategory.Should().BeNull();
        view.MonthlyPrice.Should().Be(product.MonthlyPrice);
        view.Description.Should().Be(product.Description);
        view.Metadata.Tags.Should().BeEquivalentTo(["desk", "standing"]);
        view.Metadata.Attributes["type"].Should().Be("sit-stand");
        view.Metadata.BestFor.Should().ContainSingle().Which.Should().Be("focused work");
        view.ImagePath.Should().Be(product.ImagePath);
        view.ImageAvailable.Should().Be(product.ImageAvailable);
        view.IsFeatured.Should().BeTrue();
    }

    [Fact] // CAT-54
    public void An_accessory_keeps_its_subcategory_in_the_view()
    {
        var product = new Product(
            "LMP0001",
            "A lamp",
            ProductCategory.Accessory,
            ProductSubCategory.Lamp,
            new Money(180000m, Currencies.Idr),
            "A lamp.",
            new ProductMetadata(["lamp", "warm"], new Dictionary<string, string> { ["quality"] = "warm" }, ["reading"], ["outdoor use"]),
            "/images/lamp.svg",
            IsFeatured: false,
            ImageAvailable: true);

        var view = ProductViewMapper.ToView(product);

        view.Category.Should().Be(CatalogCategory.Accessory);
        view.SubCategory.Should().Be(CatalogSubCategory.Lamp);
        view.IsFeatured.Should().BeFalse("a record without the popular badge is not featured");
    }

    [Fact] // CAT-54
    public void Every_domain_vocabulary_member_has_a_published_counterpart()
    {
        // The two vocabularies are deliberately separate, so this is what stops them drifting apart:
        // a member added to one and not the other fails here rather than at runtime on the next load.
        Enum.GetNames<ProductCategory>().Should().BeEquivalentTo(Enum.GetNames<CatalogCategory>());
        Enum.GetNames<ProductSubCategory>().Should().BeEquivalentTo(Enum.GetNames<CatalogSubCategory>());

        foreach (var category in Enum.GetValues<ProductCategory>())
        {
            var subCategory = category == ProductCategory.Accessory ? ProductSubCategory.Lamp : (ProductSubCategory?)null;

            var view = ProductViewMapper.ToView(new Product(
                "AAA0001",
                "A thing",
                category,
                subCategory,
                new Money(1m, Currencies.Idr),
                "A thing.",
                new ProductMetadata(["thing"], new Dictionary<string, string>(), [], []),
                "/images/x.svg",
                IsFeatured: false,
                ImageAvailable: true));

            view.Category.ToString().Should().Be(category.ToString());
            view.SubCategory?.ToString().Should().Be(subCategory?.ToString());
        }
    }
}
