using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Host.Configs;
using CoreRentalNet.Host.Mcp;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>The MCP envelope projects the bounded page and keeps the catalogue's total.</summary>
public sealed class CatalogueSearchToolAnswerTests
{
    [Fact]
    public void The_answer_bounds_each_description_and_keeps_the_matched_total()
    {
        var products = new[]
        {
            Product("DSK-1", new string('a', CatalogToolLimits.MaximumDescriptionCharacterCount + 1)),
            Product("DSK-2", "Short."),
        };

        var answer = CatalogueSearchToolAnswer.From(products, take: 1, cheapestProductIgnoringTheCeiling: null);

        answer.Matches.Count.Should().Be(1);
        answer.Matches.Total.Should().Be(2);
        answer.Matches.Truncated.Should().BeTrue();
        answer.Matches.Value[0].Sku.Should().Be("DSK-1");
        answer.Matches.Value[0].Description.Length.Should().Be(
            CatalogToolLimits.MaximumDescriptionCharacterCount);
    }

    private static ProductDto Product(string sku, string description)
        => new(
            sku,
            $"Product {sku}",
            CatalogCategory.Desk,
            null,
            new Money(100000m, Currencies.Idr),
            description,
            "/images/test.svg",
            true,
            false);
}
