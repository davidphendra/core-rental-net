using AwesomeAssertions;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

public sealed class CatalogueProductRetrievalNormalizerTests
{
    private readonly CatalogueProductRetrievalNormalizer _normalizer = new();

    [Fact]
    public void A_search_that_found_products_makes_the_catalogue_available()
    {
        var result = _normalizer.Normalize(new CatalogueProductRetrievalResult(
            IsAvailable: false,
            UnavailableReason: "nothing at or below 250000",
            [new WorkspaceComponentSearchOutcome(
                "search_catalogue", WorkspaceComponentCategory.Desk, 8, null)]));

        result.IsAvailable.Should().BeTrue();
        result.UnavailableReason.Should().BeNull();
    }

    [Fact]
    public void No_searches_keeps_a_genuine_unavailable_answer()
    {
        var result = _normalizer.Normalize(new CatalogueProductRetrievalResult(
            IsAvailable: false,
            "The catalogue could not be reached.",
            []));

        result.IsAvailable.Should().BeFalse();
    }
}
