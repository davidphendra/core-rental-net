using Xunit;
using AwesomeAssertions;
using CoreRentalNet.CatalogPopulate.Transform;

namespace CoreRentalNet.CatalogPopulate.Tests;

public class ProductIdentifierTests
{
    [Theory]
    [InlineData("DSK", "B09DCYF7ND", "DSKB09DCYF7N")]
    [InlineData("CHA", "B01LZEOHVN", "CHAB01LZEOHV")]
    public void The_sku_is_the_prefix_and_nine_characters(string prefix, string asin, string expected)
        => ProductIdentifier.Sku(prefix, asin).Should().Be(expected);

    [Fact]
    public void The_sku_is_never_longer_than_the_prefix_and_nine()
        => ProductIdentifier.Sku("DSK", "SHORT").Should().Be("DSKSHORT");
}