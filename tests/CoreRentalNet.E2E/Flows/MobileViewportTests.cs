using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Xunit;
using Xunit.Abstractions;

namespace CoreRentalNet.E2E.Flows;

/// <summary>
/// A phone viewport, because the layouts are explicitly responsive and the bottom navigation and
/// the collapsed builder panel are real code paths that a desktop run never touches.
/// </summary>
public sealed class MobileViewportTests(HostFixture host, ITestOutputHelper output) : E2ETest(host, output)
{
    protected override ViewportSize? Viewport => new() { Width = 390, Height = 844 };

    [Fact] // UI-07
    public async Task On_a_phone_the_bottom_navigation_shows_and_the_side_panel_does_not()
    {
        await GotoAsync("/builder");

        await Expect(Page.Locator(".bottom-nav")).ToBeVisibleAsync();
        await Expect(Page.Locator(".builder-aside")).ToBeHiddenAsync();
        // The running total lives in the flow on a phone rather than in a floating bar.
        await Expect(Page.Locator(".floating-bar")).ToBeHiddenAsync();
    }

    [Fact] // UI-07
    public async Task The_categories_are_reachable_without_the_side_panel()
    {
        await GotoAsync("/builder");

        var chips = Page.Locator("[data-testid='category-chips'] button");
        var count = await chips.CountAsync();
        Assert.True(count >= 4, $"expected the four categories as chips, found {count}");

        await Expect(chips.First).ToBeVisibleAsync();
    }

    [Fact] // NAV-01
    public async Task Summary_and_Rent_are_closed_until_something_is_assigned()
    {
        await GotoAsync("/builder");

        var summary = Page.Locator(".bottom-nav a", new PageLocatorOptions { HasTextString = "Summary" });
        var rent = Page.Locator(".bottom-nav a", new PageLocatorOptions { HasTextString = "Rent" });

        await Expect(summary).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(summary).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(rent).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(rent).ToHaveAttributeAsync("tabindex", "-1");

        await Expect(Page.Locator("button.product-card").First).ToBeEnabledAsync();
    }

    /// <summary>
    /// On a phone the side panel is not on screen, so the way in is the empty slot on the canvas.
    /// </summary>
    private async Task AssignThroughTheCanvasAsync()
    {
        await GotoAsync("/builder");
        await OpenSlotPickerAsync(".slot--monitor");
        await PickFirstCandidateAsync();
        await Expect(Page.Locator(".slot--filled")).ToHaveCountAsync(1);
    }

    [Fact] // NAV-06
    public async Task Assigning_opens_them_again()
    {
        await AssignThroughTheCanvasAsync();

        var summary = Page.Locator(".bottom-nav a", new PageLocatorOptions { HasTextString = "Summary" });
        var rent = Page.Locator(".bottom-nav a", new PageLocatorOptions { HasTextString = "Rent" });

        await Expect(summary).ToHaveAttributeAsync("aria-disabled", "false");
        await Expect(rent).ToHaveAttributeAsync("aria-disabled", "false");
    }

    [Fact] // NAV-01
    public async Task The_steps_of_the_funnel_fit_a_phone()
    {
        await AssignThroughTheCanvasAsync();

        await Page.Locator(".bottom-nav a", new PageLocatorOptions { HasTextString = "Summary" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/review"));
        await Expect(Page.Locator(".receipt")).ToBeVisibleAsync();
        await Expect(Page.Locator(".bottom-nav")).ToBeVisibleAsync();
    }
}
