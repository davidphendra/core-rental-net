using System.Security.Claims;
using AwesomeAssertions;
using CoreRentalNet.Host.Infrastructure;
using CoreRentalNet.Host.Presentation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The rule that decides who may read the catalog.
/// </summary>
public sealed class CatalogReadAuthorizationHandlerTests
{
    private const string ClaimType = "https://corerental/permissions";
    private const string ClaimValue = "read:catalog";

    [Fact] // AUTH-01
    public async Task With_no_identity_provider_configured_the_catalog_is_open()
    {
        // The demonstration's own configuration: no provider means nobody can be authenticated, so
        // there is no reader to check. Refusing here would make a demo that boots without Auth0
        // unusable, which is the state the application is designed to start in.
        var context = Context(Anonymous());

        await Handler(withProvider: false).HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact] // AUTH-02
    public async Task With_a_provider_a_guest_is_refused()
    {
        var context = Context(Anonymous());

        await Handler(withProvider: true).HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact] // AUTH-03
    public async Task A_signed_in_account_without_the_claim_is_refused()
    {
        var context = Context(SignedIn(new Claim("name", "Dewi")));

        await Handler(withProvider: true).HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact] // AUTH-04
    public async Task A_signed_in_account_with_the_claim_is_allowed()
    {
        var context = Context(SignedIn(new Claim(ClaimType, ClaimValue)));

        await Handler(withProvider: true).HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact] // AUTH-05
    public async Task A_claim_that_merely_contains_the_value_is_not_the_value()
    {
        // "read:catalog:everything" contains "read:catalog". A substring check would let it through,
        // which is the kind of authorisation that looks right until somebody composes a wider value.
        var context = Context(SignedIn(new Claim(ClaimType, "read:catalog:everything")));

        await Handler(withProvider: true).HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact] // AUTH-06
    public async Task A_provider_without_a_configured_claim_entitles_nobody()
    {
        // Fail closed: a deployment that has a provider and cannot say which claim entitles a reader
        // has an unfinished rule, and the safe reading of an unfinished rule is nobody.
        var context = Context(SignedIn(new Claim(ClaimType, ClaimValue)));

        await Handler(withProvider: true, withClaim: false).HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    private static CatalogReadAuthorizationHandler Handler(bool withProvider, bool withClaim = true)
        => new(Identity(withProvider), ClaimSettings(withClaim));

    private static IdentitySettings Identity(bool withProvider)
        => IdentitySettings.From(Configuration(withProvider
            ? new() { ["Auth0:Domain"] = "tenant.example", ["Auth0:ClientId"] = "client" }
            : []));

    private static CatalogReadClaim ClaimSettings(bool withClaim)
        => CatalogReadClaim.From(Configuration(withClaim
            ? new()
            {
                ["Authorization:CatalogRead:ClaimType"] = ClaimType,
                ["Authorization:CatalogRead:ClaimValue"] = ClaimValue,
            }
            : []));

    private static IConfiguration Configuration(Dictionary<string, string?> values)
        => new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static AuthorizationHandlerContext Context(ClaimsPrincipal principal)
        => new([new CatalogReadRequirement()], principal, resource: null);

    private static ClaimsPrincipal Anonymous() => new(new ClaimsIdentity());

    private static ClaimsPrincipal SignedIn(params Claim[] claims)
        => new(new ClaimsIdentity(claims, authenticationType: "test"));
}
