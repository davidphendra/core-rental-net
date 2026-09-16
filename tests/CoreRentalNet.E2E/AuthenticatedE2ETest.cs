using Microsoft.Playwright;
using Xunit.Abstractions;

namespace CoreRentalNet.E2E;

/// <summary>
/// The browser suite against the host that has identity on and points at the local provider.
/// </summary>
/// <remarks>
/// A second host rather than the guest one, because identity is additive and the funnel must be
/// exercised with it off as well. Signing in here goes through the provider's own page:
/// a real OIDC handshake over a real socket, with nothing intercepted.
/// </remarks>
public abstract class AuthenticatedE2ETest(HostFixture host, ITestOutputHelper output) : E2ETest(host, output)
{
    protected override string BaseUrl => Host.AuthenticatedBaseUrl;

    /// <summary>
    /// Asks for a page while signed out, follows the challenge to the provider, and chooses the
    /// account by the name the provider shows.
    /// </summary>
    protected async Task SignInAsync(string accountName, string returnPath = "/builder")
    {
        await Page.GotoAsync($"{BaseUrl}{returnPath}");

        // The provider is a second server on localhost, reached by redirect; choosing the account
        // is the only interaction the handshake needs.
        var account = Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = accountName });
        await account.WaitForAsync();
        await account.ClickAsync();
    }
}
