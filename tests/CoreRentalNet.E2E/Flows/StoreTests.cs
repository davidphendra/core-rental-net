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

    [Fact] // STORE-06
    public async Task Switching_category_does_not_move_the_page_or_drop_the_confirmation()
    {
        await GotoAsync("/extras");

        await Page.Locator("button.product-card").First.ClickAsync();
        await Expect(Page.Locator(".alert--info")).ToContainTextAsync("added to your workspace");

        var before = await Page.Locator(".grid-store").EvaluateAsync<double>("grid => grid.getBoundingClientRect().top");

        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Chairs" }).ClickAsync();
        await Expect(Page.Locator("button.product-card").Filter(new LocatorFilterOptions { HasTextString = AChair })).ToHaveCountAsync(1);

        // The confirmation is about what was added, not about the category. Dropping it moved
        // everything below it 48px up, and the grid was standing exactly where the customer left it.
        await Expect(Page.Locator(".alert--info")).ToContainTextAsync("added to your workspace");

        var after = await Page.Locator(".grid-store").EvaluateAsync<double>("grid => grid.getBoundingClientRect().top");

        after.Should().Be(before, "switching category must not move the products the customer is looking at");
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

    [Fact] // STORE-07
    public async Task The_price_does_not_overlap_the_name()
    {
        await GotoAsync("/extras");

        // The worst case in the catalogue: a long name at the largest price, in the narrowest grid
        // the design draws (four columns).
        var card = Page.Locator("button.product-card").Filter(new LocatorFilterOptions { HasTextString = "Jimbaran Executive" });
        await Expect(card).ToHaveCountAsync(1);

        var name = await card.Locator("h3").BoundingBoxAsync();
        var price = await card.Locator("h3 + span").BoundingBoxAsync();

        name.Should().NotBeNull();
        price.Should().NotBeNull();

        // Either they are side by side, or the price wrapped onto its own line below the name. They
        // are never drawn over each other, which is what the unbroken IDR amount used to do.
        var overlaps = name!.X < price!.X + price.Width
            && price.X < name.X + name.Width
            && name.Y < price.Y + price.Height
            && price.Y < name.Y + name.Height;

        overlaps.Should().BeFalse("the price and the name must not be drawn over one another");
    }
}
