using AwesomeAssertions;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Application.Catalog;
using CoreRentalNet.Modules.Catalog.Application.Queries;
using CoreRentalNet.Modules.Catalog.Domain;
using CoreRentalNet.Modules.Catalog.Infrastructure;
using Xunit;

namespace CoreRentalNet.Modules.Catalog.UnitTests;

public sealed class CatalogQueryTests
{
    [Fact] // CAT-04
    public void Listing_a_category_returns_only_that_category()
    {
        var page = new GetCatalogPageHandler(SampleCatalog.Snapshot())
            .Handle(new GetCatalogPage(Category: CatalogCategory.Chair));

        page.Should().HaveCount(1);
        page.Should().OnlyContain(item => item.Category == CatalogCategory.Chair);
    }

    [Fact] // CAT-05
    public void Listing_a_subcategory_returns_only_that_subcategory()
    {
        var page = new GetCatalogPageHandler(SampleCatalog.Snapshot())
            .Handle(new GetCatalogPage(SubCategory: CatalogSubCategory.Coffee));

        page.Should().HaveCount(1);
        page.Should().OnlyContain(item => item.SubCategory == CatalogSubCategory.Coffee);
    }

    [Fact] // CAT-06
    public void The_extras_grouping_is_coffee_and_beanbag_only()
    {
        var extras = new GetCatalogGroupHandler(SampleCatalog.Snapshot())
            .Handle(new GetCatalogGroup(CatalogGrouping.Extras));

        extras.Should().HaveCount(2);
        extras.Select(item => item.SubCategory).Should().BeEquivalentTo([CatalogSubCategory.Coffee, CatalogSubCategory.Beanbag]);
    }

    [Fact] // CAT-07
    public void The_accessories_grouping_is_monitor_lamp_and_plant_only()
    {
        var accessories = new GetCatalogGroupHandler(SampleCatalog.Snapshot())
            .Handle(new GetCatalogGroup(CatalogGrouping.Accessories));

        accessories.Should().HaveCount(3);
        accessories.Select(item => item.SubCategory).Should().BeEquivalentTo(
            [CatalogSubCategory.Monitor, CatalogSubCategory.Lamp, CatalogSubCategory.Plant]);
    }

    [Fact] // CAT-07
    public void The_two_groupings_are_disjoint_and_together_cover_the_accessory_category()
    {
        var handler = new GetCatalogGroupHandler(SampleCatalog.Snapshot());
        var catalog = SampleCatalog.Snapshot();

        var accessories = handler.Handle(new GetCatalogGroup(CatalogGrouping.Accessories));
        var extras = handler.Handle(new GetCatalogGroup(CatalogGrouping.Extras));

        accessories.Select(item => item.Sku)
            .Should().NotIntersectWith(extras.Select(item => item.Sku));

        (accessories.Count + extras.Count).Should().Be(catalog.ByCategory(ProductCategory.Accessory).Count);
    }

    [Fact] // CAT-04
    public void The_accessory_category_is_broader_than_the_accessories_grouping()
    {
        var catalog = SampleCatalog.Snapshot();

        new GetCatalogPageHandler(catalog).Handle(new GetCatalogPage(Category: CatalogCategory.Accessory))
            .Should().HaveCount(5);
    }

    [Fact] // CAT-08
    public void Featured_products_come_from_the_popular_badge()
    {
        var featured = new GetFeaturedProductsHandler(SampleCatalog.Snapshot()).Handle(new GetFeaturedProducts());

        featured.Should().HaveCount(2);
        featured.Should().OnlyContain(item => item.IsFeatured);
    }

    [Fact] // CAT-10
    public void An_unknown_or_malformed_sku_returns_null_rather_than_throwing()
    {
        var handler = new GetProductBySkuHandler(SampleCatalog.Snapshot());

        handler.Handle(new GetProductBySku("XXX0000")).Should().BeNull();
        handler.Handle(new GetProductBySku("not a sku")).Should().BeNull();
        handler.Handle(new GetProductBySku("cha0001"))!.Name.Should().Be("Chair CHA0001");
    }

    [Fact] // CAT-11
    public void Prices_are_exposed_as_rupiah_money_through_the_public_contract()
    {
        IDefineProductPrices prices = new DefineProductPrices(SampleCatalog.Snapshot());

        var price = prices.FindPrice("CHA0001");

        price.Should().NotBeNull();
        price!.MonthlyPrice.Amount.Should().Be(400000m);
        price.MonthlyPrice.Currency.Should().Be("IDR");
        price.Category.Should().Be(CatalogCategory.Chair);
        price.Sku.Should().Be("CHA0001");
    }

    [Fact] // CAT-10
    public void The_price_contract_returns_null_for_an_unknown_sku()
    {
        IDefineProductPrices prices = new DefineProductPrices(SampleCatalog.Snapshot());

        prices.FindPrice("XXX0000").Should().BeNull();
        prices.AllPrices().Should().HaveCount(7);
    }
}
