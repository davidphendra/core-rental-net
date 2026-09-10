using AwesomeAssertions;
using CoreRentalNet.Modules.Catalog.Domain;
using Xunit;

namespace CoreRentalNet.Modules.Catalog.UnitTests;

public sealed class ProductCatalogSnapshotTests
{
    [Fact] // SLOT-03
    public void Subcategories_are_the_distinct_ones_present_and_are_ordered()
    {
        SampleCatalog.Snapshot().SubCategories.Should().Equal(
            ProductSubCategory.Beanbag,
            ProductSubCategory.Coffee,
            ProductSubCategory.Lamp,
            ProductSubCategory.Monitor,
            ProductSubCategory.Plant);
    }

    [Fact]
    public void Lookup_by_sku_finds_a_product_and_returns_null_otherwise()
    {
        var catalog = SampleCatalog.Snapshot();

        catalog.Find(Sku.Of("cha0001"))!.Name.Should().Be("Chair CHA0001");
        catalog.Find(Sku.Of("XXX0000")).Should().BeNull();
    }

    [Fact]
    public void Filtering_by_category_and_subcategory_works()
    {
        var catalog = SampleCatalog.Snapshot();

        catalog.ByCategory(ProductCategory.Accessory).Should().HaveCount(5);
        catalog.ByCategory(ProductCategory.Chair).Should().HaveCount(1);
        catalog.BySubCategory(ProductSubCategory.Monitor).Should().HaveCount(1);
    }

    [Fact]
    public void Featured_returns_only_the_popular_products()
    {
        var featured = SampleCatalog.Snapshot().Featured();

        featured.Should().HaveCount(2);
        featured.Should().OnlyContain(product => product.IsFeatured);
    }

    [Fact]
    public void Results_preserve_catalog_order()
    {
        var catalog = SampleCatalog.Snapshot();

        catalog.All.Select(product => product.Sku.Value).Should().ContainInOrder(
            "CHA0001", "DSK0001", "MON0001", "LMP0001", "PLT0001", "CFE0001", "BBG0001");
    }
}
