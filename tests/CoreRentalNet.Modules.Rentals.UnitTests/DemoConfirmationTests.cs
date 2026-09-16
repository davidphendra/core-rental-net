using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Rentals.Application.Checkout;
using Xunit;

namespace CoreRentalNet.Modules.Rentals.UnitTests;

public sealed class DemoConfirmationTests
{
    [Theory] // CO-03
    [InlineData("this is a demo")]
    [InlineData("  this is a demo  ")]
    [InlineData("This Is A Demo")]
    [InlineData("THIS IS A DEMO")]
    [InlineData("\tThis is a Demo ")]
    public void The_phrase_is_trimmed_and_case_insensitive(string typed)
        => DemoConfirmation.IsSatisfied(typed).Should().BeTrue();

    [Theory] // CO-04
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("demo")]
    [InlineData("this is a demo!")]
    [InlineData("this is demo")]
    [InlineData(null)]
    public void Anything_else_is_not_accepted(string? typed)
    {
        DemoConfirmation.IsSatisfied(typed).Should().BeFalse();

        var action = () => DemoConfirmation.EnsureSatisfied(typed);
        action.Should().Throw<DomainRuleViolationException>();
    }
}
