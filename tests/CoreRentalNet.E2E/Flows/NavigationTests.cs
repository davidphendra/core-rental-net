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
    public async Task The_way_out_opens_on_a_desk_and_a_chair_and_not_before()
    {
        await AssignFirstProductAsync("Chairs");

        // One chair is not a workspace. The control stays shut and says which slot is missing.
        var ready = Page.Locator(".total-bar button");
        await Expect(ready).ToBeDisabledAsync();
        await Expect(ready).ToHaveAttributeAsync("title", "A desk is required before you can rent.");

        await AssignADeskAndAChairAsync();

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

    [Fact] // NAV-02
    public async Task The_store_offers_a_way_back_without_offering_a_way_forward()
    {
        await GotoAsync("/extras");

        await Expect(Page.Locator("button.product-card").First).ToBeEnabledAsync();

        // A way back, and no way forward: nothing on the store leads to the summary.
        await Expect(Page.Locator("aside a[href=\'/review\']")).ToHaveCountAsync(0);
    }

    [Fact] // NAV-07
    public async Task The_bag_in_the_header_opens_on_a_desk_and_a_chair_and_not_before()
    {
        await GotoAsync("/builder");

        var bag = Page.Locator("a.app-header__icon");

        await Expect(bag).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(bag).ToHaveAttributeAsync("tabindex", "-1");

        // It stays where it is while the workspace cannot be rented, which is what aria-disabled is
        // claiming: a disabled control that still navigates is worse than no control at all.
        await bag.ClickAsync(new LocatorClickOptions { Force = true });
        await Task.Delay(400);
        Page.Url.Should().EndWith("/builder");

        await AssignADeskAndAChairAsync();
        await GotoAsync("/builder");

        await Expect(bag).ToHaveAttributeAsync("aria-disabled", "false");
        await Expect(bag).ToHaveAttributeAsync("tabindex", "0");

        await bag.ClickAsync();
        await Expect(Page.Locator(".receipt")).ToBeVisibleAsync();
    }

    [Fact] // AUTH-16
    public async Task With_no_identity_provider_configured_the_builder_stays_open()
    {
        // This suite runs the application exactly as it ships: no Auth0 configuration. The policy says
        // what that means - no provider, so no reader to check - and the builder is a demonstration's
        // builder. Refusing every request here would break the demo, not protect it.
        await GotoAsync("/builder");

        await Expect(Page.Locator(".workspace-stage")).ToBeVisibleAsync();
        await Expect(Page.Locator("aside[aria-label='Selection panel']")).ToBeVisibleAsync();
    }

    [Fact] // AUTH-17
    public async Task The_page_a_refused_account_lands_on_explains_itself()
    {
        // Signing in to reach it needs a real account, so the destination is checked directly - it is
        // where the cookie options send a refused account, and a blank page there was the defect.
        await GotoAsync("/access-denied");

        await Expect(Page.Locator("h1")).ToContainTextAsync("No access to the builder");
        await Expect(Page.Locator("a[href='/account/logout']")).ToBeVisibleAsync();
    }
}
