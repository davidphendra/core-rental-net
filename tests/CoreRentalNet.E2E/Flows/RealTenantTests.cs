using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Xunit;
using Xunit.Abstractions;

namespace CoreRentalNet.E2E.Flows;

/// <summary>
/// The opt-in test against the developer's real tenant (AUTH-19). Skipped unless
/// credentials are supplied, because it needs the tenant, a real account and the public internet -
/// none of which belongs inside the hermetic suite.
/// </summary>
/// <remarks>
/// Run it with:
/// <code>
/// CORERENTAL_TENANT_EMAIL=... CORERENTAL_TENANT_PASSWORD=... \
/// dotnet test tests/CoreRentalNet.E2E --filter FullyQualifiedName~RealTenantTests
/// </code>
/// and <c>CORERENTAL_TENANT_AUDIENCE</c> when the tenant has an API whose access token carries the
/// permission the builder's gate is configured to require.
/// </remarks>
public sealed class RealTenantTests : E2ETest
{
    private readonly ITestOutputHelper log;
    private string baseUrl = string.Empty;

    public RealTenantTests(HostFixture host, ITestOutputHelper output)
        : base(host, output)
    {
        log = output;
    }

    protected override string BaseUrl => baseUrl;

    public override async Task InitializeAsync()
    {
        baseUrl = await Host.StartRealTenantHostAsync();

        await base.InitializeAsync();
    }

    [RealTenantFact] // AUTH-19
    public async Task The_tenant_signs_the_account_in_and_the_gate_answers()
    {
        var email = Environment.GetEnvironmentVariable("CORERENTAL_TENANT_EMAIL")!;
        var password = Environment.GetEnvironmentVariable("CORERENTAL_TENANT_PASSWORD")!;
        var audience = Environment.GetEnvironmentVariable("CORERENTAL_TENANT_AUDIENCE");
        var expectBuilder = !string.IsNullOrEmpty(audience)
            || string.Equals(Environment.GetEnvironmentVariable("CORERENTAL_TENANT_EXPECT_BUILDER"), "true", StringComparison.OrdinalIgnoreCase);

        await Page.GotoAsync($"{BaseUrl}/account/login?returnUrl=%2Fbuilder");

        // The provider's own Universal Login page: the handshake is real, so this is the only place
        // the credentials are used.
        var username = Page.Locator("input#username, input[name='username'], input[type='email']").First;
        await username.WaitForAsync(new LocatorWaitForOptions { Timeout = 60_000 });
        await username.FillAsync(email);
        await Page.Locator("input#password, input[name='password'], input[type='password']").First.FillAsync(password);
        await Page.Locator("button[type='submit']").First.ClickAsync();

        // A tenant with an audience may ask the account to consent to it. Clicking through is part
        // of what a person would do, and a missing page here simply means it was not asked.
        var consent = Page.Locator("button[name='action'][value='accept'], button[value='accept']").First;
        try
        {
            await consent.WaitForAsync(new LocatorWaitForOptions { Timeout = 5_000 });
            await consent.ClickAsync();
        }
        catch (TimeoutException)
        {
            // No consent asked.
        }

        try
        {
            await Page.WaitForURLAsync(
                url => url.StartsWith(BaseUrl, StringComparison.Ordinal),
                new PageWaitForURLOptions { Timeout = 60_000 });
        }
        catch (TimeoutException)
        {
            throw new InvalidOperationException($"The sign-in did not return to the app. The browser is at {Page.Url}.");
        }

        // Authentication itself worked, and it was this account: the header draws the account and
        // the profile states the address the tenant issued.
        await Expect(Page.Locator(".account-menu__initials").First).ToBeVisibleAsync();

        await Page.GotoAsync($"{BaseUrl}/profile");
        await Expect(Page.Locator(".profile").First).ToContainTextAsync(email);

        log.WriteLine($"PROFILE: {(await Page.Locator(".profile").First.InnerTextAsync()).Replace('\n', ' ')}");

        // The gate's answer for this account.
        await Page.GotoAsync($"{BaseUrl}/builder");

        if (!expectBuilder)
        {
            // No audience means Auth0 issues no JWT access token, so no permission claim can be read
            // and the gate must refuse. Asserted rather than assumed, because "the account is a
            // manager" and "the tenant can say so" are two different things.
            await Page.WaitForURLAsync(
                url => url.Contains("/access-denied", StringComparison.Ordinal),
                new PageWaitForURLOptions { Timeout = 30_000 });

            return;
        }

        // The configured claim decides, and the account carries it.
        try
        {
            await WaitForCircuitAsync(".workspace-stage");
        }
        catch (TimeoutException)
        {
            throw new InvalidOperationException(
                $"The builder did not open for the account. The browser is at {Page.Url}.");
        }
    }
}

/// <summary>
/// A fact that runs only when the tenant's credentials are in the environment, so the suite stays
/// green and offline by default.
/// </summary>
