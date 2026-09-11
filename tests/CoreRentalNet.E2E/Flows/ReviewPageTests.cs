using AwesomeAssertions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Xunit;
using Xunit.Abstractions;

namespace CoreRentalNet.E2E.Flows;

/// <summary>
/// The review page as a page. It once showed the <em>name</em> of a parameter instead of its value,
/// because a string parameter given a bare attribute value takes the words as text, and it once
/// carried the session's error about a monitor slot under the delivery address.
/// </summary>
public sealed class ReviewPageTests(HostFixture host, ITestOutputHelper output) : E2ETest(host, output)
{
    private const string AMonitor = "Ultrawide";

    [Fact] // REV-01
    public async Task The_review_page_never_shows_the_name_of_a_parameter()
    {
        await AssignADeskAndAChairAsync();
        await GotoAsync("/review");

        // The box holds the address or nothing at all - never the expression that was meant to
        // supply it.
        (await Page.Locator("#delivery-address").InputValueAsync()).Should().NotContain("Session");

        (await Page.Locator("body").InnerTextAsync()).Should().NotContain("Session.");
    }

    [Fact] // REV-02
    public async Task A_refused_item_does_not_become_an_error_about_the_delivery_address()
    {
        // Fill every monitor place, then ask for one more, which is the one refusal this UI can make.
        await GotoAsync("/builder");

        for (var place = 0; place < 3; place++)
        {
            await OpenSlotPickerAsync(".slot--monitor");
            await PickCandidateAsync(place);
        }

        await ChooseCategoryAsync("Accessories");
        await Page.Locator("aside button.product-card").Filter(new LocatorFilterOptions { HasTextString = AMonitor }).First.ClickAsync();
        await Expect(Page.Locator(".alert")).ToContainTextAsync("at most 3");

        await GotoAsync("/review");

        // The refusal was about the monitor slot. It happened there, it was said there, and the
        // delivery address must not be wearing it.
        await Expect(Page.Locator("#delivery-address")).ToHaveAttributeAsync("aria-invalid", "false");
        (await Page.Locator("body").InnerTextAsync()).Should().NotContain("Monitor slot");
    }
}
