using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Xunit;
using Xunit.Abstractions;

namespace CoreRentalNet.E2E.Flows;

/// <summary>
/// Authentication and the builder's gate, end to end: a real OIDC handshake against a provider in
/// this repository, and a browser observing what each account is allowed to see.
/// </summary>
/// <remarks>
/// The gate is also proven at the policy and handler level (CoreRentalNet.Host.Tests), and at the
/// source level (CoreRentalNet.ArchitectureTests). This is the layer that says a person signing in
/// through a browser gets the same answer, which is the part a unit test cannot reach.
/// </remarks>
public sealed class AuthenticationTests(HostFixture host, ITestOutputHelper output)
    : AuthenticatedE2ETest(host, output)
{
    [Fact] // AUTH-04, AUTH-06
    public async Task An_account_with_the_permission_reaches_the_builder()
    {
        await SignInAsync("Dewi Reader");

        await Page.WaitForURLAsync($"{BaseUrl}/builder", new PageWaitForURLOptions { Timeout = 30_000 });
        await WaitForCircuitAsync(".workspace-stage");

        // Signed in: the header draws the account, not the sign-in link, and the provider's picture
        // is never fetched.
        await Expect(Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Account menu for Dewi Reader" }))
            .ToBeVisibleAsync();

        await Expect(Page.Locator(".account-menu__initials").First).ToHaveTextAsync("DR");

        AssertNoThirdPartyRequests();
    }

    [Fact] // the gate: a signed-in account that does not carry the configured permission
    public async Task An_account_without_the_permission_is_told_the_builder_is_refused()
    {
        await SignInAsync("Bagus Guest");

        await Page.WaitForURLAsync(
            url => url.Contains("/access-denied", StringComparison.Ordinal),
            new PageWaitForURLOptions { Timeout = 30_000 });

        await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "No access to the builder" }))
            .ToBeVisibleAsync();
    }

    [Fact] // the gate: a permission is the words in the right order
    public async Task A_permission_whose_words_are_reversed_does_not_open_the_builder()
    {
        // The gate asks for read:catalog; this account holds catalog:read and no role. The check is
        // exact, so the reversed words are a different value - asserted rather than assumed, because
        // a substring or set-shaped check would let it through.
        await SignInAsync("Citra Reversed");

        await Page.WaitForURLAsync(
            url => url.Contains("/access-denied", StringComparison.Ordinal),
            new PageWaitForURLOptions { Timeout = 30_000 });
    }

    [Fact] // AUTH-33, the affordance follows the entitlement
    public async Task A_guest_is_not_offered_the_builder_link()
    {
        await Page.GotoAsync($"{BaseUrl}/");

        // Wait for the account affordance before deciding the link is absent, so the answer is not
        // read from a header whose authentication state has not arrived yet.
        await Expect(Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Sign in" }))
            .ToBeVisibleAsync();

        await Expect(Page.Locator("header .app-header__nav a[href='/builder']")).ToHaveCountAsync(0);
    }

    [Fact] // AUTH-33, the affordance follows the entitlement
    public async Task An_account_with_the_permission_is_offered_the_builder_link()
    {
        await SignInAsync("Dewi Reader");

        await Page.GotoAsync($"{BaseUrl}/");

        await Expect(Page.Locator("header .app-header__nav a[href='/builder']")).ToHaveCountAsync(1);
    }

    [Fact] // AUTH-33, the affordance follows the entitlement
    public async Task An_account_without_the_permission_is_not_offered_the_builder_link()
    {
        await SignInAsync("Bagus Guest");

        await Page.GotoAsync($"{BaseUrl}/");

        await Expect(Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Account menu for Bagus Guest" }))
            .ToBeVisibleAsync();

        await Expect(Page.Locator("header .app-header__nav a[href='/builder']")).ToHaveCountAsync(0);
    }

    [Fact] // AUTH-32, the role is read from the access token's permission
    public async Task The_profile_shows_the_role_the_access_token_names()
    {
        await SignInAsync("Citra Reversed");

        await Page.GotoAsync($"{BaseUrl}/profile");

        // Citra carries no role in the ID token; supervisor:role in the access token is the only
        // place the role is stated, so the page can only show it by reading it from there.
        await Expect(Page.Locator(".profile")).ToContainTextAsync("Supervisor");
    }

    [Fact] // AUTH-03
    public async Task A_guest_asking_for_the_builder_is_sent_to_the_provider()
    {
        await Page.GotoAsync($"{BaseUrl}/builder");

        await Page.WaitForURLAsync(
            url => url.StartsWith(Host.ProviderAuthority, StringComparison.Ordinal),
            new PageWaitForURLOptions { Timeout = 30_000 });

        await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Sign in to Core Rental" }))
            .ToBeVisibleAsync();
    }

    [Fact] // AUTH-05, AUTH-16, AUTH-24
    public async Task Signing_out_ends_the_session_and_offers_sign_in_again()
    {
        await SignInAsync("Dewi Reader");
        await Page.WaitForURLAsync($"{BaseUrl}/builder", new PageWaitForURLOptions { Timeout = 30_000 });

        await Page.GotoAsync($"{BaseUrl}/account/logout");

        await Page.WaitForURLAsync(
            url => url == $"{BaseUrl}/",
            new PageWaitForURLOptions { Timeout = 30_000 });

        await Expect(Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Sign in" }))
            .ToBeVisibleAsync();

        AssertNoThirdPartyRequests();
    }
}
