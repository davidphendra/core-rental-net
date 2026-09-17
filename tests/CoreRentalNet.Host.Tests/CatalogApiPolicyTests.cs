using AwesomeAssertions;
using CoreRentalNet.Host.Composition;
using CoreRentalNet.Host.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The catalogue API's policy as the application wires it: the same requirement the pages use, on the
/// bearer scheme, and only where there is a provider to validate a token against.
/// </summary>
/// <remarks>
/// Built through the registrations rather than by hand, so the test fails if the composition root
/// stops registering the scheme or the policy. The identity registration is used for its
/// configuration singletons; with a domain and client id present it also registers the OIDC handler
/// and the bearer scheme, neither of which is asked to answer here.
/// </remarks>
public sealed class CatalogApiPolicyTests
{
    [Fact] // API-10
    public void With_a_provider_the_policy_requires_the_requirement_on_the_bearer_scheme()
    {
        using var services = Composition(withProvider: true);

        var policy = Policy(services);

        policy.Requirements.Should().ContainSingle().Which.Should().BeOfType<ClaimRequirement>();
        policy.AuthenticationSchemes.Should().ContainSingle().Which.Should().Be(JwtBearerDefaults.AuthenticationScheme);
    }

    [Fact] // API-11
    public void With_no_provider_the_policy_names_no_scheme()
    {
        using var services = Composition(withProvider: false);

        // Nothing is registered to authenticate against, and a policy that names a scheme nobody
        // registered throws when authorization runs. With none named, the requirement opens the
        // catalogue exactly as it does for the pages.
        Policy(services).AuthenticationSchemes.Should().BeEmpty();
    }

    [Fact] // API-11
    public void The_api_policy_is_not_the_pages_policy()
    {
        using var services = Composition(withProvider: true);

        Policy(services).AuthenticationSchemes.Should().NotBeEmpty(
            "the page policy stays on the cookie; only the API names the bearer scheme");
        CatalogPolicy.Name.Should().NotBe(CatalogApiPolicy.Name);
    }

    [Fact] // API-10
    public async Task With_a_provider_and_an_audience_the_bearer_scheme_is_registered_and_bound_to_the_api()
    {
        using var services = Composition(withProvider: true);

        var scheme = await services.GetRequiredService<IAuthenticationSchemeProvider>()
            .GetSchemeAsync(JwtBearerDefaults.AuthenticationScheme);

        scheme.Should().NotBeNull("the API policy names it, so it has to exist");

        var options = services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        options.Authority.Should().Be("https://tenant.example/");
        options.Audience.Should().Be(
            "https://api.example",
            "without an audience a token minted for another API would be accepted");
    }

    [Fact] // API-11
    public async Task A_provider_with_no_audience_still_challenges_with_the_bearer_scheme_and_requires_an_audience()
    {
        using var services = Composition(withProvider: true, withAudience: false);

        var scheme = await services.GetRequiredService<IAuthenticationSchemeProvider>()
            .GetSchemeAsync(JwtBearerDefaults.AuthenticationScheme);

        scheme.Should().NotBeNull(
            "a machine caller must be refused by the bearer scheme, not redirected to a sign-in page");

        var options = services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        options.Audience.Should().BeNull();
        options.TokenValidationParameters.RequireAudience.Should().BeTrue(
            "a token whose audience was never named must be refused, not accepted on its issuer alone");
    }

    private static AuthorizationPolicy Policy(IServiceProvider services)
        => services.GetRequiredService<IOptions<AuthorizationOptions>>().Value.GetPolicy(CatalogApiPolicy.Name)!;

    private static ServiceProvider Composition(bool withProvider, bool withAudience = true)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Auth0:Enabled"] = withProvider ? "true" : "false",
            ["Auth0:Domain"] = withProvider ? "tenant.example" : null,
            ["Auth0:ClientId"] = withProvider ? "client" : null,
            ["Auth0:ClientSecret"] = withProvider ? "secret" : null,
            ["Auth0:Audience"] = withProvider && withAudience ? "https://api.example" : null,
            ["Authorization:CatalogRead:ClaimType"] = "permissions",
            ["Authorization:CatalogRead:ClaimValue"] = "read:catalog",
        };

        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(settings);

        var identity = builder.AddOptionalIdentity();
        builder.AddCatalogApiAuthentication(identity);
        builder.AddCatalogApiAuthorization(identity);

        return builder.Services.BuildServiceProvider();
    }
}
