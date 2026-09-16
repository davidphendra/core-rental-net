using AwesomeAssertions;
using CoreRentalNet.Modules.Catalog.Infrastructure;
using Xunit;

namespace CoreRentalNet.Modules.Catalog.UnitTests;

/// <summary>
/// SKU text is no longer normalised: the <c>SkuService</c> went with the value object, so a SKU is
/// the string the file holds, and lookup is the one place letter case is ignored.
/// </summary>
public sealed class SkuTests
{
    [Fact]
    public void A_sku_is_kept_exactly_as_the_file_writes_it()
    {
        using var file = new TemporaryCatalogFile(SampleCatalog.SevenRows);
        var catalog = new ProductCatalog(file.Path, null);

        catalog.All.Select(product => product.Sku).Should().Contain("CHA0001");
    }

    [Fact]
    public void The_same_sku_is_found_whatever_its_case()
    {
        using var file = new TemporaryCatalogFile(SampleCatalog.SevenRows);
        var catalog = new ProductCatalog(file.Path, null);

        catalog.Find("CHA0001")!.Sku.Should().Be(catalog.Find("cha0001")!.Sku);
    }
}
