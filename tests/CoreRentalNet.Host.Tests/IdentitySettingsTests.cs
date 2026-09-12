using AwesomeAssertions;
using CoreRentalNet.Host.Presentation;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// Whether this deployment signs anyone in, and the safe direction of the switch that says so.
/// </summary>
public sealed class IdentitySettingsTests
{
    [Theory] // AUTH-17
    [InlineData(null)]
    [InlineData("")]
    [InlineData("true")]
    [InlineData("TRUE")]
    [InlineData("yes")]
    public void Identity_is_on_unless_configuration_says_otherwise(string? configured)
        => Settings(configured).Enabled.Should().BeTrue();

    [Theory] // AUTH-18
    [InlineData("false")]
    [InlineData("FALSE")]
    [InlineData(" false ")]
    public void An_explicit_false_turns_it_off(string configured)
        => Settings(configured).Enabled.Should().BeFalse();

    [Fact] // AUTH-19
    public void Credentials_that_are_present_do_not_matter_when_it_is_off()
    {
        var settings = Settings("false");

        settings.Domain.Should().NotBeNull();
        settings.IsConfigured.Should().BeFalse("the application is running without identity on purpose");
    }

    [Fact] // AUTH-20
    public void Credentials_and_the_switch_together_are_what_configure_it()
        => Settings("true").IsConfigured.Should().BeTrue();

    private static IdentitySettings Settings(string? enabled)
        => IdentitySettings.From(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Auth0:Enabled"] = enabled,
            ["Auth0:Domain"] = "tenant.example",
            ["Auth0:ClientId"] = "client",
        }).Build());
}
