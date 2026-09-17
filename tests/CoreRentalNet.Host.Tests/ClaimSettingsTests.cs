using AwesomeAssertions;
using CoreRentalNet.Host.Presentation;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// Reading the claim the policy checks, from configuration.
/// </summary>
public sealed class ClaimSettingsTests
{
    /// <summary>The catalogue's claim, which is the one these tests were written against.</summary>
    private static CoreRentalNet.Host.Presentation.ClaimSettings Settings(IConfiguration configuration)
        => CoreRentalNet.Host.Presentation.ClaimSettings.From(configuration, CoreRentalNet.Host.Presentation.ClaimSettings.CatalogRead);

    [Fact] // AUTH-07
    public void The_claim_is_whatever_configuration_says_it_is()
    {
        var claim = Settings(Configuration(new()
        {
            ["Authorization:CatalogRead:ClaimType"] = "https://corerental/permissions",
            ["Authorization:CatalogRead:ClaimValue"] = "read:catalog",
        }));

        claim.IsConfigured.Should().BeTrue();
        claim.ClaimType.Should().Be("https://corerental/permissions");
        claim.ClaimValue.Should().Be("read:catalog");
    }

    [Theory] // AUTH-08
    [InlineData(null, "read:catalog")]
    [InlineData("https://corerental/permissions", null)]
    [InlineData("   ", "read:catalog")]
    [InlineData("https://corerental/permissions", "   ")]
    [InlineData(null, null)]
    public void Half_a_claim_is_not_a_claim(string? claimType, string? claimValue)
        => Settings(Configuration(new()
            {
                ["Authorization:CatalogRead:ClaimType"] = claimType,
                ["Authorization:CatalogRead:ClaimValue"] = claimValue,
            }))
            .IsConfigured.Should().BeFalse();

    [Fact] // AUTH-09
    public void Surrounding_space_is_not_part_of_the_value()
        => Settings(Configuration(new()
            {
                ["Authorization:CatalogRead:ClaimType"] = "  https://corerental/permissions  ",
                ["Authorization:CatalogRead:ClaimValue"] = "  read:catalog  ",
            }))
            .ClaimValue.Should().Be("read:catalog");

    private static IConfiguration Configuration(Dictionary<string, string?> values)
        => new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
