using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Catalog.Domain;
using Xunit;

namespace CoreRentalNet.Modules.Catalog.UnitTests;

public sealed class SkuTests
{
    [Theory]
    [InlineData("CHA449AGLBB0", "CHA449AGLBB0")]
    [InlineData("  cha449aglbb0  ", "CHA449AGLBB0")]
    public void Valid_skus_are_trimmed_and_upper_cased(string input, string expected)
    {
        Sku.Of(input).Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("CHA-449")]
    [InlineData("CHA 449")]
    [InlineData("CHA449AGLBB0CHA449AGLBB0CHA449AGLBB0")]
    public void Invalid_skus_are_rejected(string? input)
    {
        Sku.TryParse(input, out _).Should().BeFalse();

        var action = () => Sku.Of(input);
        action.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void Skus_compare_by_value()
    {
        Sku.Of("CHA0001").Should().Be(Sku.Of("cha0001"));
        Sku.Of("CHA0001").Should().NotBe(Sku.Of("CHA0002"));
    }
}
