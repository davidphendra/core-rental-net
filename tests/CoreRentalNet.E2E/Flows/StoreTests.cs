using AwesomeAssertions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Xunit;
using Xunit.Abstractions;

namespace CoreRentalNet.E2E.Flows;

/// <summary>
/// The store page: one category at a time, chosen by the pills above it and remembered for the
/// session. What the page used to do is the reason for the middle test here - it rendered a second
/// category's listing in the panel beside its own, so two categories were on screen at once and
/// neither said which was which.
/// </summary>
public sealed class StoreTests(HostFixture host, ITestOutputHelper output) : E2ETest(host, output)
{
    /// <summary>The catalog holds ten desks, and the beanbags are a different category entirely.</summary>
    private const string ADesk = "Canggu Bamboo";
    private const string ABeanbag = "Seminyak Floor Seat";
    private const string AChair = "Seminyak Lounge";

    [Fact] // STORE-01
    public async Task The_store_opens_on_desks_and_loads_only_them()
    {
        await GotoAsync("/extras");

        await Expect(Page.Locator("button.product-card")).ToHaveCountAsync(10);
        await Expect(Page.Locator("button.product-card").Filter(new LocatorFilterOptions { HasTextString = ADesk })).ToHaveCountAsync(1);
        await Expect(Page.Locator("button.product-card").Filter(new LocatorFilterOptions { HasTextString = ABeanbag })).ToHaveCountAsync(0);
    }

    [Fact] // STORE-02
    public async Task The_store_is_the_catalog_and_no_longer_has_a_selection_panel()
    {
        await GotoAsync("/extras");

        await Expect(Page.Locator("aside")).ToHaveCountAsync(0);
        await Expect(Page.Locator(".grid-store")).ToBeVisibleAsync();
    }

    [Fact] // STORE-03
    public async Task Choosing_a_category_replaces_the_listing_rather_than_adding_to_it()
    {
        await GotoAsync("/extras");

        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Chairs" }).ClickAsync();

        await Expect(Page.Locator("button.product-card").Filter(new LocatorFilterOptions { HasTextString = AChair })).ToHaveCountAsync(1);
        await Expect(Page.Locator("button.product-card").Filter(new LocatorFilterOptions { HasTextString = ADesk })).ToHaveCountAsync(0);

        // Ten, not twenty: the desks are gone, they were not added to.
        await Expect(Page.Locator("button.product-card")).ToHaveCountAsync(10);
    }

    [Fact] // STORE-05
    public async Task A_remembered_category_the_catalog_does_not_know_is_ignored()
    {
        // The cookie is named here rather than imported, because the suite talks to the application
        // over HTTP and should not be able to reach into it for a constant. The name is part of the
        // contract being tested.
        await Page.Context.AddCookiesAsync(
            [new Cookie { Name = "store-category", Value = "Nonsense", Url = BaseUrl }]);

        await GotoAsync("/extras");

        await Expect(Page.Locator("button.product-card")).ToHaveCountAsync(10);
        await Expect(Page.Locator("button.product-card").Filter(new LocatorFilterOptions { HasTextString = ADesk })).ToHaveCountAsync(1);
    }

    [Fact] // STORE-04
    public async Task The_category_is_remembered_for_the_session()
    {
        await GotoAsync("/extras");
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Chairs" }).ClickAsync();

        var chairs = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Chairs" });
        await Expect(chairs).ToHaveAttributeAsync("aria-pressed", "true");

        await Page.ReloadAsync();
        await Expect(Page.Locator(".grid-store")).ToBeVisibleAsync();

        // Remembered across a page load, which means it was written down somewhere the next request
        // could read - and it is a session cookie, so it is the browser that forgets it.
        await Expect(chairs).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Page.Locator("button.product-card").Filter(new LocatorFilterOptions { HasTextString = AChair })).ToHaveCountAsync(1);
        await Expect(Page.Locator("button.product-card")).ToHaveCountAsync(10);
    }
}
