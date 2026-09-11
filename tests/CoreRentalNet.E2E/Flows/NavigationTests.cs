using AwesomeAssertions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Xunit;
using Xunit.Abstractions;

namespace CoreRentalNet.E2E.Flows;

/// <summary>
/// What is and is not reachable before the workspace holds anything.
/// </summary>
public sealed class NavigationTests(HostFixture host, ITestOutputHelper output) : E2ETest(host, output)
{
    [Fact] // NAV-02, NAV-05
    public async Task On_desktop_the_way_out_of_the_builder_is_closed_until_something_is_assigned()
    {
        await GotoAsync("/builder");

        // The panel no longer carries a link to the summary, so the floating bar is the only way
        // out of the builder - and it is the thing that has to stay shut while the workspace is
        // empty. Both halves are checked, because removing a way out is only safe if it is gone.
        await Expect(Page.Locator("aside a[href=\'/review\']")).ToHaveCountAsync(0);
        await Expect(Page.Locator(".total-bar button")).ToBeDisabledAsync();
        await Expect(Page.Locator(".total-bar")).ToContainTextAsync("Rp0");
    }

    [Fact] // NAV-03
    public async Task Nothing_stops_the_customer_from_filling_a_workspace()
    {
        await GotoAsync("/builder");

        var cards = Page.Locator("button.product-card");
        await Expect(cards.First).ToBeEnabledAsync();
        (await cards.CountAsync()).Should().BeGreaterThan(0);
    }

    [Fact] // NAV-06
    public async Task Assigning_one_item_opens_the_way_out()
    {
        await AssignFirstProductAsync();

        var ready = Page.Locator(".total-bar button");
        await Expect(ready).ToBeEnabledAsync();

        // And that it leads there. An enabled control is not the same as a working one.
        await ready.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/review"));
    }

    [Fact] // NAV-04
    public async Task Reaching_the_review_page_directly_with_an_empty_workspace_sends_you_back()
    {
        await GotoAsync("/builder", ".workspace-stage");

        await Page.GotoAsync($"{BaseUrl}/review");

        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/builder\\?empty=true"));
        await Expect(Page.Locator(".alert--info")).ToContainTextAsync("before reviewing");
    }

    [Fact] // NAV-07
    public async Task The_shopping_bag_is_decorative()
    {
        await GotoAsync("/");

        var bag = Page.Locator(".app-header__icon");

        (await bag.EvaluateAsync<string>("element => element.tagName")).Should().Be("SPAN");
        await Expect(bag).ToHaveAttributeAsync("aria-label", "Shopping bag, decorative");
    }

    [Fact] // NAV-02
    public async Task The_store_offers_a_way_back_without_offering_a_way_forward()
    {
        await GotoAsync("/extras");

        await Expect(Page.Locator("button.product-card").First).ToBeEnabledAsync();

        // A way back, and no way forward: nothing on the store leads to the summary.
        await Expect(Page.Locator("aside a[href=\'/review\']")).ToHaveCountAsync(0);
    }
}
