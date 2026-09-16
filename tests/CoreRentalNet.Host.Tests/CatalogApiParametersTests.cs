using AwesomeAssertions;
using CoreRentalNet.Host.Controllers;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The catalogue's filter vocabulary: which words a filter accepts, and what a caller is told when it
/// sends one that is not among them. Unit level, because it is a decision about words rather than a
/// step in the request.
/// </summary>
public sealed class CatalogApiParametersTests
{
    [Theory] // API-02
    [InlineData("desk")]
    [InlineData("Desk")]
    [InlineData("DESK")]
    [InlineData("  desk  ")]
    public void A_category_is_matched_ignoring_case_and_surrounding_space(string value)
    {
        CatalogApiParameters.TryMatch<CatalogCategory>(value, out var matched).Should().BeTrue();
        matched.Should().Be(CatalogCategory.Desk);
    }

    [Fact] // API-02
    public void A_subcategory_is_matched_ignoring_case()
    {
        CatalogApiParameters.TryMatch<CatalogSubCategory>("Monitor", out var matched).Should().BeTrue();
        matched.Should().Be(CatalogSubCategory.Monitor);
    }

    [Theory] // API-01, API-02
    [InlineData("sofa")]
    [InlineData("1")]
    [InlineData("0")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void A_word_the_catalogue_does_not_publish_matches_nothing(string? value)
        => CatalogApiParameters.TryMatch<CatalogCategory>(value, out _).Should().BeFalse();

    [Fact] // API-01
    public void The_vocabulary_is_the_words_the_wire_writes()
        => CatalogApiParameters.Names<CatalogCategory>().Should().Equal("chair", "desk", "accessory");

    [Fact] // API-01
    public void A_refusal_names_the_word_that_was_sent_and_the_words_that_would_have_worked()
    {
        var refusal =
            CatalogApiParameters.Refusal("category", "  sofa  ", CatalogApiParameters.Names<CatalogCategory>());

        refusal.Should().Contain("sofa").And.Contain("chair").And.Contain("desk").And.Contain("accessory");
        refusal.Should().NotContain("  sofa  ", "the word is repeated trimmed, not padded with what was typed");
    }

    [Fact] // API-01
    public void An_unknown_subcategory_is_refused_with_the_subcategory_vocabulary()
        => CatalogApiParameters
            .Refusal("subCategory", "hammock", CatalogApiParameters.Names<CatalogSubCategory>())
            .Should()
            .Contain("hammock")
            .And.Contain("monitor")
            .And.Contain("lamp")
            .And.Contain("plant")
            .And.Contain("beanbag");
}
