using AwesomeAssertions;
using CoreRentalNet.Host.Configs;
using CoreRentalNet.Host.Mcp;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>The one rule that bounds a description published by a catalogue MCP tool.</summary>
public sealed class CatalogueDescriptionPolicyTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void An_absent_description_becomes_empty(string? description)
    {
        CatalogueDescriptionPolicy.Truncate(description).Should().BeEmpty();
    }

    [Fact]
    public void A_description_at_the_limit_is_unchanged()
    {
        var description = new string('a', CatalogToolLimits.MaximumDescriptionCharacterCount);

        CatalogueDescriptionPolicy.Truncate(description).Should().Be(description);
    }

    [Fact]
    public void A_longer_description_is_cut_to_the_limit()
    {
        var description = new string('a', CatalogToolLimits.MaximumDescriptionCharacterCount + 20);

        var truncated = CatalogueDescriptionPolicy.Truncate(description);

        truncated.Should().Be(
            new string('a', CatalogToolLimits.MaximumDescriptionCharacterCount));
    }

    [Fact]
    public void An_emoji_at_the_boundary_is_not_split()
    {
        var description = string.Concat(Enumerable.Repeat("😀", 120));

        var truncated = CatalogueDescriptionPolicy.Truncate(description);

        truncated.Length.Should().BeLessThanOrEqualTo(CatalogToolLimits.MaximumDescriptionCharacterCount);
        truncated.Should().NotContain("\uFFFD");
    }

    [Fact]
    public void A_combining_sequence_is_not_split()
    {
        var description = string.Concat(Enumerable.Repeat("e\u0301", 120));

        var truncated = CatalogueDescriptionPolicy.Truncate(description);

        truncated.Length.Should().BeLessThanOrEqualTo(CatalogToolLimits.MaximumDescriptionCharacterCount);
    }
}
