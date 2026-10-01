using System.Security.Claims;
using AwesomeAssertions;
using CoreRentalNet.Host.Extentions;
using CoreRentalNet.Host.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The gate as the application actually wires it: the policy named by the page, answered by the
/// handler through the container the composition root builds.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ClaimAuthorizationHandlerTests"/> proves the rule. But the authorization
/// service knows nothing about this policy until <c>AddCatalogAuthorization</c> registers it, and
/// nothing yet asserted that the name
/// <see cref="CatalogPolicy.Name"/> resolves, that the requirement reaches the handler, or that the
/// handler's dependencies arrive from the same configuration the host reads. A policy that is
/// registered under a different name, or not at all, denies everyone - which is the safe failure, but
/// it is not the behaviour anyone intended and no unit test would see it.
/// </para>
/// <para>
/// Built through <see cref="AuthorizationRegistrationExtentions.AddCatalogAuthorization"/> and
/// <see cref="IdentityRegistrationExtentions.AddOptionalIdentity"/> rather than by hand, so the test fails if
/// the composition root stops registering either. The identity registration is used for its
/// configuration singletons; with a domain and client id present it also registers the OIDC handler,
/// which is never asked to answer here.
/// </para>
/// </remarks>
public sealed class CatalogAuthorizationPolicyTests
{
    private const string ClaimType = "https://corerental/permissions";
    private const string ClaimValue = "read:catalog";

    [Fact] // AUTH-04, through the real policy
    public async Task The_account_carrying_the_configured_claim_is_let_into_the_builder()
    {
        using var services = Composition(withProvider: true);
        var principal = SignedIn(new Claim(ClaimType, ClaimValue));

        var result = await AuthorizeAsync(services, principal);

        result.Succeeded.Should().BeTrue();
    }

    [Fact] // AUTH-03, AUTH-05, through the real policy
    public async Task An_account_without_the_exact_claim_is_refused()
    {
        using var services = Composition(withProvider: true);

        var refused = new[]
        {
            // Signed in, no permission at all.
            SignedIn(new Claim("name", "Dewi")),
            // The words the same, in the other order. The gate is exact, so this is a different value.
            SignedIn(new Claim(ClaimType, "catalogService:read")),
            // Contains the configured value rather than being it.
            SignedIn(new Claim(ClaimType, "read:catalog:everything")),
            // The value under a claim type nobody configured.
            SignedIn(new Claim("https://example/permissions", ClaimValue)),
            // Not signed in at all.
            new ClaimsPrincipal(new ClaimsIdentity()),
        };

        foreach (var principal in refused)
        {
            var result = await AuthorizeAsync(services, principal);

            result.Succeeded.Should().BeFalse(
                "the gate admits the configured claim and nothing else");
        }
    }

    [Fact] // AUTH-01, through the real policy
    public async Task A_deployment_with_no_provider_leaves_the_catalog_open()
    {
        using var services = Composition(withProvider: false);

        var result = await AuthorizeAsync(services, new ClaimsPrincipal(new ClaimsIdentity()));

        result.Succeeded.Should().BeTrue();
    }

    private static Task<AuthorizationResult> AuthorizeAsync(IServiceProvider services, ClaimsPrincipal principal)
        => services.GetRequiredService<IAuthorizationService>()
            .AuthorizeAsync(principal, resource: null, CatalogPolicy.Name);

    private static ClaimsPrincipal SignedIn(params Claim[] claims)
        => new(new ClaimsIdentity(claims, authenticationType: "test"));

    /// <summary>The container the way the host builds it, with only the two registrations that matter.</summary>
    private static ServiceProvider Composition(bool withProvider)
    {
        var settings = withProvider
            ? new Dictionary<string, string?>
            {
                ["Auth0:Enabled"] = "true",
                ["Auth0:Domain"] = "tenant.example",
                ["Auth0:ClientId"] = "client",
                ["Auth0:ClientSecret"] = "secret",
                ["Authorization:CatalogRead:ClaimType"] = ClaimType,
                ["Authorization:CatalogRead:ClaimValue"] = ClaimValue,
            }
            : new Dictionary<string, string?>
            {
                ["Auth0:Enabled"] = "false",
                ["Authorization:CatalogRead:ClaimType"] = ClaimType,
                ["Authorization:CatalogRead:ClaimValue"] = ClaimValue,
            };

        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(settings);

        builder.AddOptionalIdentity();
        builder.AddCatalogAuthorization();

        return builder.Services.BuildServiceProvider();
    }
}
