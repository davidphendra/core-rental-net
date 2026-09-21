using AwesomeAssertions;
using CoreRentalNet.Host.Composition;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The tuning knobs, and what happens when one of them is wrong.
/// </summary>
/// <remarks>
/// A knob is read as text and falls back rather than throwing, which is the rule <c>SuggestionSpread.From</c>
/// already sets: a typo in a tuning value must not stop the application from starting, because that is a failure
/// nobody would connect to the setting that caused it. A value that can only mean a mistake — a half-life of
/// zero, a negative weight — is refused for the same reason rather than obeyed.
/// </remarks>
public sealed class SelectionSignalSettingsTests
{
    private static DiscoverySettings From(params (string Key, string Value)[] configured)
        => DiscoverySettings.From(new ConfigurationBuilder()
            .AddInMemoryCollection(configured.Select(pair => new KeyValuePair<string, string?>(pair.Key, pair.Value)))
            .Build());

    [Fact] // SCR-29
    public void A_deployment_that_has_tuned_nothing_gets_the_shipped_defaults()
    {
        var settings = DiscoverySettings.From(new ConfigurationBuilder().Build());

        settings.PerBucket.Should().Be(DiscoverySettings.DefaultPerBucket);
        settings.BoostWeight.Should().Be(DiscoverySettings.DefaultBoostWeight);
        settings.HalfLifeDays.Should().Be(DiscoverySettings.DefaultHalfLifeDays);
    }

    [Fact] // SCR-29
    public void A_usable_value_is_taken_as_given()
    {
        var settings = From(
            ("Discovery:PerBucket", "3"),
            ("Discovery:BoostWeight", "0.25"),
            ("Discovery:HalfLifeDays", "7"));

        settings.PerBucket.Should().Be(3);
        settings.BoostWeight.Should().Be(0.25);
        settings.HalfLifeDays.Should().Be(7);
    }

    [Theory] // SCR-29
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("half a day")]
    [InlineData("")]
    public void A_value_that_can_only_be_a_mistake_falls_back(string unusable)
    {
        // Zero would decay everything instantly and a negative half-life would decay UPWARDS, so neither is
        // obeyed: the default is used and the application starts.
        var settings = From(("Discovery:HalfLifeDays", unusable), ("Discovery:BoostWeight", unusable));

        settings.HalfLifeDays.Should().Be(DiscoverySettings.DefaultHalfLifeDays);
        settings.BoostWeight.Should().Be(DiscoverySettings.DefaultBoostWeight);
    }

    [Fact] // SCR-29
    public void The_ten_percent_default_is_small_enough_to_be_a_tie_breaker()
    {
        // Stated as a test because it is a decision rather than a number: the signal only ever reorders products
        // similarity already chose, and a weight at or above one would let a well-liked product sit above a far
        // better match. If somebody raises it, this is the line that asks them to think about it.
        DiscoverySettings.DefaultBoostWeight.Should().BeLessThan(1);
    }
}
