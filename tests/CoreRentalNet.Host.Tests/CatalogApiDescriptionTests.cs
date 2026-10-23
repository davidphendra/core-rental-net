using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Host.Configs;
using CoreRentalNet.Host.Presentation;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>The REST answer keeps the catalogue's full description; only the MCP read model is bounded.</summary>
public sealed class CatalogApiDescriptionTests
{
    private const string LongDescription =
        "A long catalogue description that is deliberately longer than the MCP tool boundary allows, "
        + "because the REST answer is a catalogue page and must keep the full text.";

    [Fact]
    public void The_REST_answer_keeps_the_full_description()
    {
        var product = new ProductDto(
            "DSKB0B6MCK87",
            "HON Mod Desk Shell, 60 x 30 x 29, Slate Teak",
            CatalogCategory.Desk,
            null,
            new Money(271000m, Currencies.Idr),
            LongDescription,
            "/images/vendored/DSKB0B6MCK87.jpg",
            true,
            false);

        var answer = CatalogAnswer.Of([product], 1);

        answer.Value[0].Description.Should().Be(LongDescription);
        answer.Value[0].Description.Length.Should().BeGreaterThan(
            CatalogToolLimits.MaximumDescriptionCharacterCount);
    }
}
