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

    [Fact] // AUTH-26
    public void The_role_claim_defaults_to_the_namespace_this_project_documents()
        => Settings("true").RoleClaimType.Should().Be(CurrentCustomer.DefaultRoleClaimType);

    [Fact] // AUTH-26
    public void A_tenant_whose_action_uses_another_namespace_says_so_in_configuration()
        => IdentitySettings.From(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Auth0:Domain"] = "tenant.example",
            ["Auth0:ClientId"] = "client",
            ["Auth0:RoleClaimType"] = "https://contoso.example/roles",
        }).Build()).RoleClaimType.Should().Be("https://contoso.example/roles");

    [Fact] // AUTH-30
    public void No_audience_means_no_access_token_is_asked_for()
        => Settings("true").Audience.Should().BeNull();

    [Fact] // AUTH-30
    public void An_audience_names_the_api_whose_access_token_carries_the_permissions()
        => IdentitySettings.From(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Auth0:Domain"] = "tenant.example",
            ["Auth0:ClientId"] = "client",
            ["Auth0:Audience"] = "  https://corerental/api  ",
        }).Build()).Audience.Should().Be("https://corerental/api");

    [Fact] // AUTH-31
    public void The_scope_defaults_to_the_profile_the_application_records()
        => Settings("true").Scope.Should().Be(IdentitySettings.DefaultScope);

    [Fact] // AUTH-31
    public void A_deployment_whose_api_demands_a_scope_names_it_beside_the_audience()
        => IdentitySettings.From(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Auth0:Domain"] = "tenant.example",
            ["Auth0:ClientId"] = "client",
            ["Auth0:Scope"] = "  openid profile email catalog:read  ",
        }).Build()).Scope.Should().Be("openid profile email catalog:read");

    [Fact] // ADR-0020, the browser suite's authority
    public void A_named_authority_replaces_the_one_the_domain_implies()
        => IdentitySettings.From(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Auth0:Domain"] = "tenant.example",
            ["Auth0:ClientId"] = "client",
            ["Auth0:Authority"] = "  http://127.0.0.1:5199/  ",
        }).Build()).Authority.Should().Be("http://127.0.0.1:5199", "a trailing slash would not match the issuer");

    [Fact] // ADR-0020
    public void Without_a_named_authority_the_domain_decides()
        => Settings("true").Authority.Should().BeNull();

    private static IdentitySettings Settings(string? enabled)
        => IdentitySettings.From(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Auth0:Enabled"] = enabled,
            ["Auth0:Domain"] = "tenant.example",
            ["Auth0:ClientId"] = "client",
        }).Build());
}
