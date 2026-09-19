using System.Security.Claims;
using AwesomeAssertions;
using CoreRentalNet.Host.Composition;
using CoreRentalNet.Host.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// AIWB-20, AIWB-21 and AIWB-24: the AI permission through the real policy, and the sharpest statement of
/// what e05s06 changed — two permissions, one handler, and opposite answers when nothing is configured.
/// </summary>
/// <remarks>
/// <b>What this asserts and what it does not.</b> "The AI section is absent" is a browser fact: only the
/// rendered page can prove no section was offered. What is asserted here is the decision the section is
/// built on — the policy refuses — and `e05s07` owns the half that renders it.
/// </remarks>
public sealed class AiAuthorizationPolicyTests
{
    private const string ClaimType = "https://corerental/permissions";
    private const string ClaimValue = "use:ai";

    [Fact] // AIWB-20
    public async Task The_account_carrying_the_configured_claim_may_use_the_builder()
    {
        using var services = Composition(withProvider: true, withAiClaim: true);

        var result = await AuthorizeAsync(services, SignedIn(new Claim(ClaimType, ClaimValue)));

        result.Succeeded.Should().BeTrue();
    }

    [Fact] // AIWB-20
    public async Task A_signed_in_account_without_the_claim_may_not()
    {
        using var services = Composition(withProvider: true, withAiClaim: true);

        var withoutIt = await AuthorizeAsync(services, SignedIn(new Claim("name", "Dewi")));
        var nearly = await AuthorizeAsync(services, SignedIn(new Claim(ClaimType, "use:ai:everything")));

        withoutIt.Succeeded.Should().BeFalse("signing in is not the same as being entitled");
        nearly.Succeeded.Should().BeFalse("the comparison is exact, as it is for the catalogue");
    }

    [Fact] // AIWB-21
    public async Task A_deployment_that_has_not_named_the_claim_shows_no_ai_section()
    {
        // A provider, and an account that would carry the claim if anyone had said which one it is. The
        // safe reading of an unfinished rule is nobody - so this is refused, not opened.
        using var services = Composition(withProvider: true, withAiClaim: false);

        var result = await AuthorizeAsync(services, SignedIn(new Claim(ClaimType, ClaimValue)));

        result.Succeeded.Should().BeFalse();
    }

    [Fact] // AIWB-21, and the whole point of the story
    public async Task With_no_identity_provider_the_ai_permission_is_closed_while_the_catalogue_stays_open()
    {
        // The catalogue is a demonstration anyone may read; the builder spends money. One handler answers
        // both, and it is the requirement - not the handler - that decides which way they fall.
        using var services = Composition(withProvider: false, withAiClaim: true);
        var nobody = new ClaimsPrincipal(new ClaimsIdentity());

        (await AuthorizeAsync(services, nobody, AiPolicy.Name)).Succeeded
            .Should().BeFalse("the AI feature is closed when the deployment has not been finished");

        (await AuthorizeAsync(services, nobody, CatalogPolicy.Name)).Succeeded
            .Should().BeTrue("and the catalogue behaves exactly as it did before this story");
    }

    [Fact] // AIWB-24
    public async Task The_catalogue_s_own_rule_still_admits_its_configured_claim()
    {
        // The regression in the other direction: adding a second permission must not disturb the first.
        using var services = Composition(withProvider: true, withAiClaim: true);

        (await AuthorizeAsync(services, SignedIn(new Claim(ClaimType, "read:catalog")), CatalogPolicy.Name)).Succeeded
            .Should().BeTrue();

        (await AuthorizeAsync(services, SignedIn(new Claim(ClaimType, "read:catalog")), AiPolicy.Name)).Succeeded
            .Should().BeFalse("the catalogue's claim does not buy the builder");
    }

    /// <summary>The AI permission, which is what almost every case here is about.</summary>
    private static Task<AuthorizationResult> AuthorizeAsync(IServiceProvider services, ClaimsPrincipal principal)
        => AuthorizeAsync(services, principal, AiPolicy.Name);

    private static Task<AuthorizationResult> AuthorizeAsync(
        IServiceProvider services,
        ClaimsPrincipal principal,
        string policy)
        => services.GetRequiredService<IAuthorizationService>()
            .AuthorizeAsync(principal, resource: null, policy);

    private static ClaimsPrincipal SignedIn(params Claim[] claims)
        => new(new ClaimsIdentity(claims, authenticationType: "test"));

    /// <summary>The container the way the host builds it, with only the registrations that matter.</summary>
    private static ServiceProvider Composition(bool withProvider, bool withAiClaim)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Auth0:Enabled"] = withProvider ? "true" : "false",
            ["Auth0:Domain"] = "tenant.example",
            ["Auth0:ClientId"] = "client",
            ["Auth0:ClientSecret"] = "secret",
            ["Authorization:CatalogRead:ClaimType"] = ClaimType,
            ["Authorization:CatalogRead:ClaimValue"] = "read:catalog",
        };

        if (withAiClaim)
        {
            settings["Authorization:AIUse:ClaimType"] = ClaimType;
            settings["Authorization:AIUse:ClaimValue"] = ClaimValue;
        }

        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(settings);

        builder.AddOptionalIdentity();
        builder.AddCatalogAuthorization();
        builder.AddAiAuthorization();

        return builder.Services.BuildServiceProvider();
    }
}
