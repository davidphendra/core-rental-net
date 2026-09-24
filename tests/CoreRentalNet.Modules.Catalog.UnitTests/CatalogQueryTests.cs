using AwesomeAssertions;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Infrastructure;
using Xunit;

namespace CoreRentalNet.Modules.Catalog.UnitTests;

public sealed class CatalogQueryTests
{
    [Fact] // CAT-04
    public void Listing_a_category_returns_only_that_category()
    {
        using var file = new TemporaryCatalogFile(SampleCatalog.SevenRows);
        var catalog = new ProductCatalogService(file.Path, null);

        var page = catalog.ByCategory(CatalogCategory.Chair);

        page.Should().HaveCount(1);
        page.Should().OnlyContain(item => item.Category == CatalogCategory.Chair);
    }

    [Fact] // CAT-05
    public void Listing_a_subcategory_returns_only_that_subcategory()
    {
        using var file = new TemporaryCatalogFile(SampleCatalog.SevenRows);
        var catalog = new ProductCatalogService(file.Path, null);

        var page = catalog.BySubCategory(CatalogSubCategory.Coffee);

        page.Should().HaveCount(1);
        page.Should().OnlyContain(item => item.SubCategory == CatalogSubCategory.Coffee);
    }

    [Fact] // CAT-04
    public void The_accessory_category_is_every_subcategory_the_accessory_entry_shows()
    {
        using var file = new TemporaryCatalogFile(SampleCatalog.SevenRows);
        var catalog = new ProductCatalogService(file.Path, null);

        // One entry now carries all five kinds of accessory, which is the invariant the panel's
        // grouping rests on: a kind missing here would be missing from the tab as well.
        var accessories = catalog.ByCategory(CatalogCategory.Accessory);

        accessories.Should().HaveCount(5);
        accessories.Select(item => item.SubCategory).Should().BeEquivalentTo(
        [
            CatalogSubCategory.Monitor,
            CatalogSubCategory.Lamp,
            CatalogSubCategory.Plant,
            CatalogSubCategory.Coffee,
            CatalogSubCategory.Beanbag,
        ]);
    }

    [Fact] // CAT-08
    public void Featured_products_come_from_the_popular_badge()
    {
        using var file = new TemporaryCatalogFile(SampleCatalog.SevenRows);
        var catalog = new ProductCatalogService(file.Path, null);

        var featured = catalog.Featured();

        featured.Should().HaveCount(2);
        featured.Should().OnlyContain(item => item.IsFeatured);
    }

    [Fact] // CAT-10
    public void An_unknown_sku_returns_null_rather_than_throwing()
    {
        using var file = new TemporaryCatalogFile(SampleCatalog.SevenRows);
        var catalog = new ProductCatalogService(file.Path, null);

        catalog.Find("XXX0000").Should().BeNull();
        catalog.Find("cha0001")!.Name.Should().Be("Chair CHA0001");
    }

    [Fact] // CAT-13
    public void A_search_stays_inside_the_category_it_was_asked_about()
    {
        using var file = new TemporaryCatalogFile(SampleCatalog.SevenRows);
        var catalog = new ProductCatalogService(file.Path, null);

        catalog.Search(CatalogCategory.Desk, null).Should()
            .OnlyContain(item => item.Category == CatalogCategory.Desk);

        catalog.Search(CatalogCategory.Accessory, CatalogSubCategory.Lamp).Should()
            .OnlyContain(item => item.SubCategory == CatalogSubCategory.Lamp);
    }

    [Fact] // CAT-14
    public void A_search_with_no_filters_returns_every_product()
    {
        using var file = new TemporaryCatalogFile(SampleCatalog.SevenRows);
        var catalog = new ProductCatalogService(file.Path, null);

        catalog.Search(null, null).Should().HaveCount(7);
    }
}
