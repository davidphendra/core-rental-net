using AwesomeAssertions;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Infrastructure;
using Xunit;

namespace CoreRentalNet.Modules.Catalog.UnitTests;

public sealed class ProductCatalogTests
{
    private static ProductCatalog Repository(out TemporaryCatalogFile file)
    {
        file = new TemporaryCatalogFile(SampleCatalog.SevenRows);
        return new ProductCatalog(file.Path, null);
    }

    [Fact]
    public void Lookup_by_sku_finds_a_product_whatever_its_case_and_returns_null_otherwise()
    {
        var catalog = Repository(out var file);
        using (file)
        {
            catalog.Find("cha0001")!.Name.Should().Be("Chair CHA0001");
            catalog.Find("XXX0000").Should().BeNull();
        }
    }

    [Fact]
    public void Filtering_by_category_and_subcategory_works()
    {
        var catalog = Repository(out var file);
        using (file)
        {
            catalog.ByCategory(CatalogCategory.Accessory).Should().HaveCount(5);
            catalog.ByCategory(CatalogCategory.Chair).Should().HaveCount(1);
            catalog.BySubCategory(CatalogSubCategory.Monitor).Should().HaveCount(1);
        }
    }

    [Fact]
    public void Featured_returns_only_the_popular_products()
    {
        var catalog = Repository(out var file);
        using (file)
        {
            var featured = catalog.Featured();

            featured.Should().HaveCount(2);
            featured.Should().OnlyContain(product => product.IsFeatured);
        }
    }

    [Fact]
    public void Results_preserve_catalog_order()
    {
        var catalog = Repository(out var file);
        using (file)
        {
            catalog.All.Select(product => product.Sku).Should().ContainInOrder(
                "CHA0001", "DSK0001", "MON0001", "LMP0001", "PLT0001", "CFE0001", "BBG0001");
        }
    }
}
