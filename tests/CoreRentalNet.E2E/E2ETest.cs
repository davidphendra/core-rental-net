using System.Collections.Concurrent;
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

    protected virtual string BaseUrl => Host.BaseUrl;

    protected virtual ViewportSize? Viewport => null;

    public virtual async Task InitializeAsync()
    {
        Page = await Host.NewPageAsync(Viewport);

        // Every test asserts its own requests stay on this origin; see HermeticityTests.
        _requests.Clear();
        Page.Request += (_, request) => _requests.Enqueue(request.Url);

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

    private readonly ConcurrentQueue<string> _requests = new();

    /// <summary>
    /// The requests the page has made, read as a snapshot.
    /// </summary>
    /// <remarks>
    /// The browser reports each request on its own thread while a test reads this from the test's, and
    /// the page is usually still fetching images when the assertion runs - which is exactly the moment
    /// the funnel test asserts. A list here is a race, and its failure ("collection was modified")
    /// names nothing about the application; a concurrent queue is the collection for a writer on one
    /// thread and a reader on another.
    /// </remarks>
    protected IReadOnlyList<string> Requests => [.. _requests];

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
    /// A workspace that can be rented. A desk and a chair are mandatory, so anything about checkout
    /// has to start from one rather than from a single item.
    /// </summary>
    protected async Task AssignADeskAndAChairAsync()
    {
        await AssignFirstProductAsync("Desks");
        await ChooseCategoryAsync("Chairs");
        await Page.Locator("button.product-card").First.ClickAsync();

        await Expect(Page.Locator(".slot--filled")).ToHaveCountAsync(2);
    }

    /// <summary>
    /// Chooses a category tab. The tabs are icons only, so they are addressed by their accessible
    /// name, which is what a screen reader announces and is the only thing left to go on.
    /// </summary>
    protected Task ChooseCategoryAsync(string name)
        => Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = name, Exact = true }).ClickAsync();

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

    /// <summary>
    /// Clicks an empty box on the canvas and waits for its picker.
    /// </summary>
    /// <remarks>
    /// This used to take a position to click at, because the chair stood over the middle of the
    /// desk's box and the middle of a desk belonged to the chair - so a caller that meant the desk
    /// had to say where the desk was visible. The chair now stops at the desk's baseline, so the
    /// middle of every box belongs to that box.
    /// </remarks>
    protected async Task OpenSlotPickerAsync(string slotClass)
    {
        // The monitor slot holds three boxes, so "the empty box for this slot" is the first one.
        await Page.Locator($"{slotClass}.slot--empty").First.ClickAsync();

        await Expect(Page.Locator("dialog[open] .picker")).ToBeVisibleAsync();
    }

    /// <summary>
    /// Picks a candidate from the open picker and waits for the dialog to go. Without the wait the
    /// next click can land on a dialog that is still on its way out, which looks like an unclickable
    /// canvas rather than a race.
    /// </summary>
    protected async Task PickCandidateAsync(int index)
    {
        await Page.Locator("dialog[open] button.product-card").Nth(index).ClickAsync();
        await Expect(Page.Locator("dialog[open]")).ToHaveCountAsync(0);
    }

    protected Task PickFirstCandidateAsync() => PickCandidateAsync(0);

    // ---------------------------------------------------------------- review and checkout

    protected async Task FillAddressAsync(string address = "Villa Lotus, Canggu")
    {
        await Page.Locator("#delivery-address").FillAsync(address);
        await Page.Locator("#delivery-address").BlurAsync();
        await Expect(Page.Locator("#delivery-address")).ToHaveValueAsync(address);
    }

    protected async Task OpenDemoDialogAsync()
    {
        // The design puts the way to rent outside the summary card, at the width of the column, so it is
        // found on the page rather than inside the card.
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Rent This Setup" }).ClickAsync();
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
