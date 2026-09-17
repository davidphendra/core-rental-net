using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Xunit;
using Xunit.Abstractions;

namespace CoreRentalNet.E2E.Flows;

/// <summary>
/// Choosing a candidate and letting it replace the workspace.
/// </summary>
/// <remarks>
/// The destructive step, and the only place this feature writes anything. The three things that make it
/// safe are each asserted here: it asks when there is something to lose, it does not ask when there is
/// not, and what it keeps is kept because it was never in the payload.
/// </remarks>
public sealed class AiBuilderApplyTests(HostFixture host, ITestOutputHelper output) : AiBuilderTest(host, output)
{
    private const string Address = "Villa Lotus, Canggu";

    /// <summary>AIB-18 — a workspace with something in it asks before replacing.</summary>
    [Fact]
    public async Task AIB_18_a_non_empty_workspace_asks_before_replacing()
    {
        await SignedInAsync(PowerAccount);
        await AssignADeskAndAChairAsync();
        await ScenarioAsync("essential");

        await RunAsync();

        await ApplyAsync("low");

        // The dialog says what is at stake rather than asking a bare question.
        var body = Page.Locator("[data-testid='apply-confirm-body']");

        await Expect(body).ToBeVisibleAsync();
        await Expect(body).ToContainTextAsync("Essential");
        await Expect(body).ToContainTextAsync("delivery address is kept");

        // Cancelling leaves the workspace exactly as it was.
        await Page.Locator("[data-testid='apply-cancel']").ClickAsync();

        await Expect(Page.Locator("dialog[open]")).ToHaveCountAsync(0);
        await Expect(Page.Locator(".slot--filled")).ToHaveCountAsync(2);
    }

    /// <summary>AIB-20 — an empty workspace applies without a dialog.</summary>
    /// <remarks>
    /// A confirmation that guards nothing is a step a customer learns to click through, and it would
    /// train them to dismiss the one that matters.
    /// </remarks>
    [Fact]
    public async Task AIB_20_an_empty_workspace_applies_without_a_dialog()
    {
        await SignedInAsync(PowerAccount);
        await ScenarioAsync("essential");

        await Expect(Page.Locator(".slot--filled")).ToHaveCountAsync(0);

        await RunAsync();
        await ApplyAsync("low");

        await Expect(Page.Locator("dialog[open]")).ToHaveCountAsync(0);

        // The composition is the workspace: one desk, one chair and one monitor.
        await Expect(Page.Locator(".slot--desk.slot--filled")).ToHaveCountAsync(1);
        await Expect(Page.Locator(".slot--chair.slot--filled")).ToHaveCountAsync(1);
        await Expect(Page.Locator(".slot--monitor.slot--filled")).ToHaveCountAsync(1);
        await Expect(Page.Locator(".slot--filled")).ToHaveCountAsync(3);

        // And the candidates went with the workspace they were composed for.
        await Expect(Page.Locator("[data-testid='ai-outcome']")).ToHaveCountAsync(0);
    }

    /// <summary>AIB-19 — confirming replaces the slots and leaves the delivery address alone.</summary>
    /// <remarks>
    /// The address survives because it was never in the payload, and the test proves it the only way that
    /// counts: by setting one first, applying over it, and going to look.
    /// </remarks>
    [Fact]
    public async Task AIB_19_confirming_replaces_the_slots_and_leaves_the_address()
    {
        await SignedInAsync(PowerAccount);
        await AssignADeskAndAChairAsync();

        await GotoAsync("/review");
        await FillAddressAsync(Address);

        await GotoAsync("/builder");

        await Expect(Page.Locator(".slot--filled")).ToHaveCountAsync(2);

        await ScenarioAsync("essential");

        await RunAsync();
        await ApplyAsync("high");

        await Page.Locator("[data-testid='apply-confirm']").ClickAsync();

        await Expect(Page.Locator("dialog[open]")).ToHaveCountAsync(0);

        // What was there is gone and the composition is what remains: three slots, not five.
        await Expect(Page.Locator(".slot--filled")).ToHaveCountAsync(3);
        await Expect(Page.Locator(".slot--desk.slot--filled")).ToHaveCountAsync(1);
        await Expect(Page.Locator(".slot--monitor.slot--filled")).ToHaveCountAsync(1);

        await GotoAsync("/review");

        await Expect(Page.Locator("#delivery-address")).ToHaveValueAsync(Address);
    }

    private async Task RunAsync()
    {
        await AskAsync(ADeskAChairAMonitor);

        await Expect(Options).ToHaveCountAsync(3, new LocatorAssertionsToHaveCountOptions { Timeout = 20_000 });
    }

    private Task ApplyAsync(string tier)
        => Page.Locator($"[data-testid='ai-apply'][data-tier='{tier}']").ClickAsync();
}
