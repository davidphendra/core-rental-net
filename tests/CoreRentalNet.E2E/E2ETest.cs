using AwesomeAssertions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Xunit;
using Xunit.Abstractions;

namespace CoreRentalNet.E2E;

/// <summary>
/// One browser context per test, which is one browser profile, which is one draft cookie.
/// That is how tests stay independent without restarting the application between them.
/// </summary>
[Collection(E2ECollection.Name)]
public abstract class E2ETest(HostFixture host, ITestOutputHelper output) : IAsyncLifetime
{

    protected HostFixture Host { get; } = host;

    protected IPage Page { get; private set; } = null!;

    protected string BaseUrl => Host.BaseUrl;

    protected virtual ViewportSize? Viewport => null;

    public async Task InitializeAsync()
    {
        Page = await Host.NewPageAsync(Viewport);

        // Every test asserts its own requests stay on this origin; see HermeticityTests.
        Requests.Clear();
        Page.Request += (_, request) => Requests.Add(request.Url);

        // A Blazor circuit that fails to start looks exactly like a slow page. Forwarding the
        // browser's own console and unhandled errors turns that into a diagnosable failure.
        Page.Console += (_, message) =>
        {
            if (message.Type is "error" or "warning")
            {
                output.WriteLine($"[console:{message.Type}] {message.Text}");
            }
        };

        Page.PageError += (_, error) => output.WriteLine($"[pageerror] {error}");
    }

    public async Task DisposeAsync() => await Page.Context.DisposeAsync();

    protected List<string> Requests { get; } = [];

    /// <summary>
    /// Navigates and waits for the circuit to be attached.
    /// </summary>
    /// <remarks>
    /// The prerendered HTML arrives before the interactive circuit is connected, and a click
    /// during that window does nothing at all. Waiting for something that only the interactive
    /// render produces is the reliable signal, and it is why each route declares one.
    /// </remarks>
    protected async Task GotoAsync(string path, string? waitFor = null)
    {
        await Page.GotoAsync($"{BaseUrl}{path}");
        await WaitForCircuitAsync(waitFor ?? SignalFor(path));
    }

    private static string SignalFor(string path) => path switch
    {
        "/builder" => ".workspace-stage",
        "/review" => ".receipt",
        "/extras" => ".grid-store",
        _ => "#main",
    };

    protected async Task WaitForCircuitAsync(string selector)
    {
        // The prerender and the circuit produce the same markup, so the only reliable way to know
        // a click will be handled is a marker that only the interactive render sets.
        await Expect(Page.Locator("[data-interactive='true']").First)
            .ToBeAttachedAsync(new LocatorAssertionsToBeAttachedOptions { Timeout = 30_000 });

        await Expect(Page.Locator(selector).First).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
    }

    // ---------------------------------------------------------------- builder

    /// <summary>
    /// Chooses a category tab. The tabs are icons only, so they are addressed by their accessible
    /// name, which is what a screen reader announces and is the only thing left to go on.
    /// </summary>
    protected Task ChooseCategoryAsync(string name)
        => Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = name }).ClickAsync();

    /// <summary>
    /// Assigns the first item of a category. Tests that care which kind of item they got say so:
    /// one that inherits whatever category happens to be open breaks the moment the order changes,
    /// which is what happened when desks became the first tab.
    /// </summary>
    protected async Task AssignFirstProductAsync(string? category = null)
    {
        await GotoAsync("/builder");

        if (category is not null)
        {
            await ChooseCategoryAsync(category);
        }

        await Page.Locator("button.product-card").First.ClickAsync();
        await Expect(Page.Locator(".slot--filled")).ToHaveCountAsync(1);
    }

    protected async Task OpenSlotPickerAsync(string slotClass)
    {
        await Page.Locator($"{slotClass}.slot--empty").ClickAsync();
        await Expect(Page.Locator("dialog[open] .picker")).ToBeVisibleAsync();
    }

    protected Task PickFirstCandidateAsync()
        => Page.Locator("dialog[open] button.product-card").First.ClickAsync();

    // ---------------------------------------------------------------- review and checkout

    protected async Task FillAddressAsync(string address = "Villa Lotus, Canggu")
    {
        await Page.Locator("#delivery-address").FillAsync(address);
        await Page.Locator("#delivery-address").BlurAsync();
        await Expect(Page.Locator("#delivery-address")).ToHaveValueAsync(address);
    }

    protected async Task OpenDemoDialogAsync()
    {
        await Page.Locator(".receipt").GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Rent This Setup" }).ClickAsync();
        await Expect(Page.Locator("dialog[open]")).ToBeVisibleAsync();
    }

    protected ILocator DialogConfirmButton
        => Page.Locator("dialog[open]").GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Rent This Setup" });

    protected static string CountOf(string cssClass, string page) => $"{page}: {cssClass}";

    protected void AssertNoThirdPartyRequests()
    {
        var origin = new Uri(BaseUrl);

        var foreign = Requests
            .Where(url => !url.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
                          && !url.StartsWith("blob:", StringComparison.OrdinalIgnoreCase))
            .Select(url => new Uri(url))
            .Where(uri => !string.Equals(uri.Host, origin.Host, StringComparison.OrdinalIgnoreCase))
            .Select(uri => uri.Host)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreign.Should().BeEmpty("the browser suite must reach nothing but the application itself");
    }
}
