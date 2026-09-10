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

        var summary = Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "View Setup Summary" });
        await Expect(summary).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(summary).ToHaveAttributeAsync("tabindex", "-1");

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

        var summary = Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "View Setup Summary" });
        await Expect(summary).ToHaveAttributeAsync("aria-disabled", "false");
        await Expect(summary).ToHaveAttributeAsync("tabindex", "0");
        await Expect(Page.Locator(".total-bar button")).ToBeEnabledAsync();
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

        var summary = Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "View Setup Summary" });
        await Expect(summary).ToHaveAttributeAsync("aria-disabled", "true");
    }
}
