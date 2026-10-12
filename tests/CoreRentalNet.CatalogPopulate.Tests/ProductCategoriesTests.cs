using Xunit;
using AwesomeAssertions;
using CoreRentalNet.CatalogPopulate.Selection;
using CoreRentalNet.CatalogPopulate.Sources;

namespace CoreRentalNet.CatalogPopulate.Tests;

public class ProductCategoriesTests
{
    [Theory]
    [InlineData("Office Products|Office Furniture & Lighting|Desks & Workstations", "Desk")]
    [InlineData("Office Products|Office Furniture & Lighting|Chairs & Sofas", "Chair")]
    [InlineData("Electronics|Computers & Accessories|Monitors", "Monitor")]
    [InlineData("Tools & Home Improvement|Lighting & Ceiling Fans|Lamps", "Lamp")]
    [InlineData("Home & Kitchen|Home Décor Products|Artificial Plants", "Plant")]
    [InlineData("Home & Kitchen|Kitchen & Dining|Coffee Machines", "Coffee")]
    [InlineData("Home & Kitchen|Furniture|Bean Bags", "Beanbag")]
    public void A_source_row_matches_its_category(string path, string expected)
    {
        var meta = new AmazonMetaRecord { Title = "x", Categories = [.. path.Split('|')] };

        ProductCategories.Match(meta)!.Key.Should().Be(expected);
    }

    [Fact]
    public void An_office_desk_is_not_mistaken_for_a_lamp()
    {
        var meta = new AmazonMetaRecord
        {
            Title = "Standing Desk",
            Categories = ["Office Products", "Office Furniture & Lighting", "Desks & Workstations"],
        };

        ProductCategories.Match(meta)!.Key.Should().Be("Desk");
    }

    [Fact]
    public void A_desk_chair_is_a_chair()
    {
        var meta = new AmazonMetaRecord { Title = "Desk Chair", Categories = ["Office Products"] };

        ProductCategories.Match(meta)!.Key.Should().Be("Chair");
    }
}