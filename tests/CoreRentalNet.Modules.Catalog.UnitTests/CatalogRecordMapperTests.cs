using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Domain;
using CoreRentalNet.Modules.Catalog.Infrastructure.Loader;
using CoreRentalNet.Modules.Catalog.Infrastructure.ProductImage;
using Xunit;

namespace CoreRentalNet.Modules.Catalog.UnitTests;

/// <summary>
/// The mapping from one catalog row to a product, tested from rows alone: the loader reads no file
/// shape other than the text it is given.
/// </summary>
public sealed class CatalogRecordMapperTests
{
    [Fact] // CAT-52
    public void A_complete_record_becomes_a_product()
    {
        var product = Single(
            """[{ "skuNo": "DSK0001", "name": "A desk", "category": "desk", "pricePerMonth": 800000, "description": "A desk.", "image": "/images/desk.svg" }]""");

        product.Sku.Should().Be("DSK0001");
        product.Name.Should().Be("A desk");
        product.Category.Should().Be(ProductCategory.Desk);
        product.SubCategory.Should().BeNull();
        product.MonthlyPrice.Amount.Should().Be(800000m);
        product.MonthlyPrice.Currency.Should().Be("IDR");
    }

    [Fact] // CAT-52
    public void A_price_without_a_currency_is_stated_in_the_settlement_currency()
        => Single("""[{ "skuNo": "DSK0001", "name": "A desk", "category": "desk", "pricePerMonth": 800000, "description": "A desk.", "image": "/images/desk.svg" }]""")
            .MonthlyPrice.Currency.Should().Be(Currencies.Idr);

    [Fact] // CAT-52
    public void A_currency_is_read_and_normalised()
        => Single("""[{ "skuNo": "DSK0001", "name": "A desk", "category": "desk", "pricePerMonth": 800000, "currency": "idr", "description": "A desk.", "image": "/images/desk.svg" }]""")
            .MonthlyPrice.Currency.Should().Be(Currencies.Idr);

    [Fact] // CAT-52
    public void A_field_written_in_a_different_case_is_still_read()
    {
        // The file writes lowercase words; the enum members are capitalized. This is the mismatch
        // that used to reject the whole file.
        var product = Single(
            """[{ "skuNo": "ACC0001", "name": "A lamp", "category": "accessory", "subCategory": "lamp", "pricePerMonth": 100, "description": "A lamp.", "image": "/i.svg" }]""");

        product.Category.Should().Be(ProductCategory.Accessory);
        product.SubCategory.Should().Be(ProductSubCategory.Lamp);
    }

    [Fact] // CAT-52
    public void A_subcategory_is_read_when_it_is_there_and_left_empty_when_it_is_not()
    {
        Single("""[{ "skuNo": "DSK0001", "name": "A desk", "category": "desk", "pricePerMonth": 100, "description": "d", "image": "/i.svg" }]""")
            .SubCategory.Should().BeNull();

        Single("""[{ "skuNo": "ACC0001", "name": "A lamp", "category": "accessory", "subCategory": "lamp", "pricePerMonth": 100, "description": "d", "image": "/i.svg" }]""")
            .SubCategory.Should().Be(ProductSubCategory.Lamp);
    }

    [Fact] // CAT-52
    public void The_popular_badge_is_read()
        => Single("""[{ "skuNo": "DSK0001", "name": "A desk", "category": "desk", "pricePerMonth": 100, "description": "d", "image": "/i.svg", "badge": "popular" }]""")
            .IsFeatured.Should().BeTrue();

    [Fact] // CAT-52
    public void A_product_without_a_badge_is_not_featured()
        => Single("""[{ "skuNo": "DSK0001", "name": "A desk", "category": "desk", "pricePerMonth": 100, "description": "d", "image": "/i.svg" }]""")
            .IsFeatured.Should().BeFalse();

    [Fact] // CAT-53
    public void The_image_resolver_decides_the_path_and_whether_it_is_there()
    {
        using var file = new TemporaryCatalogFile(
            """[{ "skuNo": "DSK0001", "name": "A desk", "category": "desk", "pricePerMonth": 100, "description": "d", "image": "https://example.test/desk.png" }]""");

        var images = new StubProductImage(new ResolvedProductImage("/images/vendored/DSK0001.svg", Available: true));

        var product = ProductLoader.Read(file.Path, images).Single();

        product.ImagePath.Should().Be("/images/vendored/DSK0001.svg");
        product.ImageAvailable.Should().BeTrue();
    }

    private static Product Single(string json)
    {
        using var file = new TemporaryCatalogFile(json);

        return ProductLoader.Read(file.Path, new StubProductImage(new ResolvedProductImage("/images/x.svg", true))).Single();
    }
}
