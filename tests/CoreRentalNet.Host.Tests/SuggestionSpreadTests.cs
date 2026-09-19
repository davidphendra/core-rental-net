using AwesomeAssertions;
using CoreRentalNet.Host.AiBuilder;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// AIWB-35: how far apart options have to be before they are shown as a range at all.
/// </summary>
/// <remarks>
/// The rule is on the RANGE and nothing else - the dearest against the cheapest - because a middle margin can
/// only ever refuse an option the range had already accepted. What happens when the range cannot be met is
/// the validator's, and is asserted in SuggestionValidationTests.
/// </remarks>
public sealed class SuggestionSpreadTests
{
    [Fact]
    public void Options_far_enough_apart_are_a_range()
    {
        var spread = new SuggestionSpread(1.5m);

        spread.Holds(cheapest: 100m, dearest: 150m).Should().BeTrue("exactly the factor is far enough");
        spread.Holds(cheapest: 100m, dearest: 200m).Should().BeTrue();
    }

    [Fact]
    public void Options_too_close_together_are_not_a_range()
    {
        var spread = new SuggestionSpread(1.5m);

        spread.Holds(cheapest: 100m, dearest: 149.99m).Should().BeFalse("a penny short is short");
        spread.Holds(cheapest: 100m, dearest: 120m).Should().BeFalse();
    }

    [Fact] // the evaluation tier tunes this without a rebuild
    public void The_factor_comes_from_configuration()
    {
        Configured("1.2").Factor.Should().Be(1.2m);

        // And it is the configured factor that decides, not the default.
        Configured("1.2").Holds(100m, 120m).Should().BeTrue();
        Configured("1.5").Holds(100m, 120m).Should().BeFalse();
    }

    [Fact]
    public void A_deployment_that_has_tuned_nothing_gets_the_shipped_factor()
    {
        SuggestionSpread.From(new ConfigurationBuilder().Build()).Factor
            .Should().Be(SuggestionSpread.DefaultFactor);
    }

    [Theory] // a tuning knob must not stop the application from starting
    [InlineData("nonsense")]
    [InlineData("")]
    [InlineData("-2")]
    [InlineData("0")]
    [InlineData("0.5")]
    public void A_factor_that_cannot_be_used_falls_back_rather_than_throwing(string configured)
    {
        // Below one is refused with the rest: it can only ever accept everything, so it is a mistake rather
        // than a choice, and a typo in a knob is not worth a deployment that will not boot.
        Configured(configured).Factor.Should().Be(SuggestionSpread.DefaultFactor);
    }

    [Fact]
    public void The_factor_is_read_without_regard_to_the_machine_s_culture()
    {
        // A decimal separator that is a comma on this machine and a point in the file would otherwise parse as
        // a different number, or not at all - and the difference would only appear on somebody else's laptop.
        Configured("1,5").Factor.Should().Be(
            SuggestionSpread.DefaultFactor,
            "a comma is not the invariant separator, so the value is unusable rather than misread as 15");
    }

    private static SuggestionSpread Configured(string factor)
        => SuggestionSpread.From(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Ai:SpreadFactor"] = factor })
            .Build());
}
