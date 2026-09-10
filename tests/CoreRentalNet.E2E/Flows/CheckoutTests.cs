using AwesomeAssertions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Xunit;

namespace CoreRentalNet.E2E.Flows;

/// <summary>The funnel, from a filled workspace to a confirmation the customer can return to.</summary>
public sealed class CheckoutTests(HostFixture host) : E2ETest(host)
{
    [Fact] // CO-01
    public async Task Renting_opens_a_dialog_that_says_nothing_is_charged()
    {
        await AssignFirstProductAsync();
        await GotoAsync("/review");
        await FillAddressAsync();

        await OpenDemoDialogAsync();

        await Expect(Page.Locator("dialog[open]")).ToContainTextAsync("No money is taken");
        await Expect(Page.Locator("#demo-confirmation")).ToBeVisibleAsync();
    }

    [Fact] // CO-02, CO-03, CO-04
    public async Task The_confirm_control_follows_the_phrase_and_nothing_else()
    {
        await AssignFirstProductAsync();
        await GotoAsync("/review");
        await FillAddressAsync();
        await OpenDemoDialogAsync();

        await Expect(DialogConfirmButton).ToBeDisabledAsync();

        await Page.Locator("#demo-confirmation").FillAsync("this is a demo!");
        await Expect(DialogConfirmButton).ToBeDisabledAsync();

        await Page.Locator("#demo-confirmation").FillAsync("  This Is A DEMO ");
        await Expect(DialogConfirmButton).ToBeEnabledAsync();
    }

    [Fact] // CO-05
    public async Task Cancelling_leaves_the_workspace_exactly_as_it_was()
    {
        await AssignFirstProductAsync();
        await GotoAsync("/review");
        await FillAddressAsync();
        var before = await Page.Locator(".receipt").InnerTextAsync();

        await OpenDemoDialogAsync();
        await Page.Locator("dialog[open]").GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Cancel" }).ClickAsync();

        await Expect(Page.Locator("dialog[open]")).ToHaveCountAsync(0);
        var after = await Page.Locator(".receipt").InnerTextAsync();
        after.Should().Be(before);
        await Expect(Page.Locator("#delivery-address")).ToHaveValueAsync("Villa Lotus, Canggu");
    }

    [Fact] // ADDR-01
    public async Task Renting_without_an_address_is_refused_inside_the_dialog()
    {
        await AssignFirstProductAsync();
        await GotoAsync("/review");

        await OpenDemoDialogAsync();
        await Page.Locator("#demo-confirmation").FillAsync("this is a demo");
        await DialogConfirmButton.ClickAsync();

        await Expect(Page.Locator("dialog[open]")).ToContainTextAsync("delivery address");
        await Expect(Page).Not.ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/orders/"));
    }

    [Fact] // CO-06, CO-07, CO-10, ORD-01, ORD-03, ORD-05
    public async Task Confirming_places_the_order_empties_the_cart_and_shows_the_order()
    {
        await AssignFirstProductAsync();
        var cookieBefore = (await Page.Context.CookiesAsync()).Single(cookie => cookie.Name == "corerental.draft").Value;

        await GotoAsync("/review");
        await FillAddressAsync();
        await OpenDemoDialogAsync();
        await Page.Locator("#demo-confirmation").FillAsync("  This Is A DEMO ");
        await DialogConfirmButton.ClickAsync();

        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/orders/CR-\\d{4}-\\d{4}"));

        // The suite shares one database, so the number depends on how many orders other tests
        // placed. Read it from the page rather than assuming it.
        var orderNumber = new System.Text.RegularExpressions.Regex("/orders/(CR-\\d{4}-\\d{4})")
            .Match(Page.Url).Groups[1].Value;

        orderNumber.Should().NotBeEmpty();
        await Expect(Page.Locator("body")).ToContainTextAsync(orderNumber);
        await Expect(Page.Locator("body")).ToContainTextAsync("Villa Lotus, Canggu");
        await Expect(Page.Locator("body")).ToContainTextAsync("Qty:");
        await Expect(Page.Locator("body")).ToContainTextAsync("Delivery & Setup");
        await Expect(Page.Locator("body")).ToContainTextAsync("Grand Total");
        await Expect(Page.Locator("body")).ToContainTextAsync("First month due today");
        await Expect(Page.Locator("body")).ToContainTextAsync("no payment was taken");
        await Expect(Page.Locator("body")).ToContainTextAsync("paid");

        var cookieAfter = (await Page.Context.CookiesAsync()).Single(cookie => cookie.Name == "corerental.draft").Value;
        cookieAfter.Should().NotBe(cookieBefore, "the cart is emptied by rotating the draft token");

        var confirmationUrl = Page.Url;
        confirmationUrl.Should().NotContain("newDraft", "the redirect strips the flag so a refresh does not rotate again");

        await GotoAsync("/builder");
        await Expect(Page.Locator(".slot--filled")).ToHaveCountAsync(0);

        await Page.GotoAsync(confirmationUrl);
        await WaitForCircuitAsync("#main");
        await Expect(Page.Locator("body")).ToContainTextAsync(orderNumber);
    }

    [Fact] // DR-04
    public async Task The_draft_cookie_is_not_readable_by_script()
    {
        await GotoAsync("/builder");

        var cookie = (await Page.Context.CookiesAsync()).Single(candidate => candidate.Name == "corerental.draft");

        cookie.HttpOnly.Should().BeTrue();
        cookie.SameSite.Should().Be(SameSiteAttribute.Lax);
        (await Page.EvaluateAsync<bool>("() => document.cookie.includes('corerental.draft')"))
            .Should().BeFalse("an HttpOnly cookie is invisible to script");

        // Secure is set only over HTTPS, which is correct and why this suite asserts it elsewhere.
        cookie.Secure.Should().BeFalse("this suite runs over plain HTTP");
    }

    [Fact] // ORD-02, SEC-06
    public async Task A_guessed_link_discloses_nothing()
    {
        await AssignFirstProductAsync();
        await GotoAsync("/review");
        await FillAddressAsync();
        await OpenDemoDialogAsync();
        await Page.Locator("#demo-confirmation").FillAsync("this is a demo");
        await DialogConfirmButton.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/orders/"));

        await Page.GotoAsync($"{BaseUrl}/orders/CR-2026-9999?t=not-a-real-token");
        await WaitForCircuitAsync("#main");

        await Expect(Page.Locator("body")).ToContainTextAsync("Order not found");
        await Expect(Page.Locator("body")).Not.ToContainTextAsync("Villa Lotus");
        await Expect(Page.Locator("body")).Not.ToContainTextAsync("Rp");
    }

    // CO-11 and CO-12 have no browser half: there is no HTTP surface to post to, and the UI
    // simply keeps the control disabled, which NavigationTests already asserts. The commands
    // themselves reject an empty or repeated checkout, and that is covered by unit and
    // integration tests rather than pretended here.
}
