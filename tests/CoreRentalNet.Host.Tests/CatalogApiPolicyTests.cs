using AwesomeAssertions;
using CoreRentalNet.Host.Composition;
using CoreRentalNet.Host.Infrastructure;
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

        policy.Requirements.Should().ContainSingle().Which.Should().BeOfType<CatalogReadRequirement>();
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

    private static AuthorizationPolicy Policy(IServiceProvider services)
        => services.GetRequiredService<IOptions<AuthorizationOptions>>().Value.GetPolicy(CatalogApiPolicy.Name)!;

    private static ServiceProvider Composition(bool withProvider)
    {
        var settings = withProvider
            ? new Dictionary<string, string?>
            {
                ["Auth0:Enabled"] = "true",
                ["Auth0:Domain"] = "tenant.example",
                ["Auth0:ClientId"] = "client",
                ["Auth0:ClientSecret"] = "secret",
                ["Auth0:Audience"] = "https://api.example",
                ["Authorization:CatalogRead:ClaimType"] = "permissions",
                ["Authorization:CatalogRead:ClaimValue"] = "read:catalog",
            }
            : new Dictionary<string, string?>
            {
                ["Auth0:Enabled"] = "false",
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
