using Xunit;
using System.Text.Json;
using AwesomeAssertions;
using CoreRentalNet.CatalogPopulate.Transform;

namespace CoreRentalNet.CatalogPopulate.Tests;

public class PriceRuleTests
{
    [Theory]
    [InlineData(199.66, 266000)]
    [InlineData(283.08, 377000)]
    [InlineData(2.68, 4000)]
    public void The_rule_matches_the_recorded_rate(double usd, int expected)
        => PriceRule.MonthlyIdr(usd).Should().Be(expected);

    [Fact]
    public void The_rule_reproduces_every_committed_price()
    {
        var path = RepoPaths.ProductsJson();
        if (path is null)
        {
            return;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var checkedRows = 0;

        foreach (var row in document.RootElement.EnumerateArray())
        {
            var price = row.GetProperty("metadata").GetProperty("attributes").GetProperty("priceusd").GetString();
            var monthly = row.GetProperty("pricePerMonth").GetInt32();

            PriceRule.MonthlyIdr(double.Parse(price!, System.Globalization.CultureInfo.InvariantCulture))
                .Should().Be(monthly);

            checkedRows++;
        }

        checkedRows.Should().Be(205);
    }
}