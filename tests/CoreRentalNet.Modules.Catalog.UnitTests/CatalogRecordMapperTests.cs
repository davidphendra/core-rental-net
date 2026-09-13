using AwesomeAssertions;
using CoreRentalNet.Modules.Catalog.Domain;
using CoreRentalNet.Modules.Catalog.Infrastructure.Loading;
using Xunit;

namespace CoreRentalNet.Modules.Catalog.UnitTests;

/// <summary>
/// The rules about what one catalog record has to say, tested from records alone: the mapper reads no
/// file, so none of these needs one.
/// </summary>
public sealed class CatalogRecordMapperTests
{
    private const string Source = "products.json";

    [Fact] // CAT-52
    public void A_complete_record_becomes_a_product()
    {
        var product = Mapper().Map(Record(), Source);

        product.Sku.Value.Should().Be("DSK0001");
        product.Name.Should().Be("A desk");
        product.Category.Should().Be(ProductCategory.Desk);
        product.MonthlyPrice.Amount.Should().Be(800000m);
    }

    [Fact] // CAT-52
    public void A_subcategory_is_read_when_it_is_there_and_left_empty_when_it_is_not()
    {
        Mapper().Map(Record(), Source).SubCategory.Should().BeNull();

        Mapper()
            .Map(Record(sku: "ACC0001", category: "accessory", subCategory: "lamp"), Source)
            .SubCategory.Should().Be(ProductSubCategory.Lamp);
    }

    [Fact] // CAT-52
    public void The_popular_badge_is_read()
        => Mapper().Map(Record(badge: "popular"), Source).IsFeatured.Should().BeTrue();

    [Theory] // CAT-52
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a sku")]
    public void A_record_without_a_usable_sku_is_refused(string? sku)
        => Refuses(Record(sku: sku), "invalid skuNo");

    [Fact] // CAT-52
    public void An_unknown_category_is_refused_and_named()
        => Refuses(Record(category: "sofa"), "unknown category 'sofa'");

    [Fact] // CAT-52
    public void An_unknown_subcategory_is_refused_and_named()
        => Refuses(Record(sku: "ACC0001", category: "accessory", subCategory: "hammock"), "unknown subcategory 'hammock'");

    [Fact] // CAT-52
    public void An_unknown_badge_is_refused_and_named()
        => Refuses(Record(badge: "shiny"), "unknown badge 'shiny'");

    [Fact] // CAT-52
    public void A_record_without_an_image_is_refused()
        => Refuses(Record(image: "   "), "no image path");

    [Fact] // CAT-52
    public void The_image_resolver_decides_the_path_and_whether_it_is_there()
    {
        var images = new StubImages(new ResolvedImage("/images/vendored/DSK0001.svg", Available: true));

        var product = new CatalogRecordMapper(images).Map(
            Record(image: "https://example.test/desk.png"),
            Source);

        product.ImagePath.Should().Be("/images/vendored/DSK0001.svg");
        product.ImageAvailable.Should().BeTrue();
    }

    private static CatalogRecordMapper Mapper()
        => new(new StubImages(new ResolvedImage("/images/desk.svg", Available: true)));

    private static void Refuses(CatalogFileRecord record, string expected)
    {
        var map = () => Mapper().Map(record, Source);

        map.Should().Throw<CatalogLoadException>().WithMessage($"*{expected}*");
    }

    private static CatalogFileRecord Record(
        string? sku = "DSK0001",
        string? name = "A desk",
        string? category = "desk",
        string? subCategory = null,
        decimal price = 800000m,
        string? image = "/images/desk.svg",
        string? badge = null)
        => new()
        {
            SkuNo = sku,
            Name = name,
            Category = category,
            SubCategory = subCategory,
            PricePerMonth = price,
            Description = "A desk.",
            Image = image,
            Badge = badge,
        };

    private sealed class StubImages(ResolvedImage answer) : IProductImages
    {
        public ResolvedImage Resolve(Sku sku, string imagePath) => answer;
    }
}
