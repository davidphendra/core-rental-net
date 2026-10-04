using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Host.Configs;
using CoreRentalNet.Host.Presentation;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>The tool answer's description cap: shorter than the API's, cut at a word, and never applied to the API.</summary>
/// <remarks>
/// The cap exists because a tool answer is spent on a model's context and the agent's reranker reads no more than
/// 320 characters of a description. The API's compact answer is a published shape and stays uncapped, which is the
/// assertion that keeps this from becoming an API change by accident.
/// </remarks>
public sealed class CatalogToolDescriptionTests
{
    [Fact]
    public void A_long_description_is_cut_to_the_limit()
    {
        var description = new string('a', CatalogToolLimits.MaximumDescriptionCharacterCount + 50);

        var trimmed = CompactCatalogProjection.Trimmed(description, CatalogToolLimits.MaximumDescriptionCharacterCount);

        trimmed.Should().HaveLength(CatalogToolLimits.MaximumDescriptionCharacterCount);
    }

    [Fact]
    public void A_description_is_cut_at_a_word_boundary()
        => CompactCatalogProjection.Trimmed("a b c dddd", maximumDescriptionCharacterCount: 6).Should().Be("a b c");

    [Fact]
    public void A_short_description_is_returned_unchanged()
    {
        const string description = "A compact desk.";

        CompactCatalogProjection.Trimmed(description, CatalogToolLimits.MaximumDescriptionCharacterCount)
            .Should().Be(description);
    }

    [Fact]
    public void A_tool_answer_caps_every_description()
    {
        var answer = CatalogAnswer.CompactForTools(
            [Product(new string('x', 900))],
            limit: 1,
            [Product(new string('x', 900))],
            CatalogToolLimits.MaximumDescriptionCharacterCount);

        answer.Value.Single().Description.Length.Should().Be(CatalogToolLimits.MaximumDescriptionCharacterCount);
    }

    [Fact]
    public void The_compact_API_answer_keeps_the_full_description()
    {
        // Non-vacuity, and the guard on the published shape: the cap is the tools', and the API's answer is
        // asserted field by field elsewhere, so it must not acquire one.
        var answer = CatalogAnswer.Compact([Product(new string('x', 900))], limit: 1, [Product(new string('x', 900))]);

        answer.Value.Single().Description.Should().HaveLength(900);
    }

    private static ProductView Product(string description)
        => new(
            "DSK0001",
            "Canggu Bamboo",
            CatalogCategory.Desk,
            null,
            new Money(420_000m, Currencies.Idr),
            description,
            new CatalogMetadata([], new Dictionary<string, string>(), [], []),
            "/images/desk.svg",
            true,
            false);
}
