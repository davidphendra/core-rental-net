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
    private const string AMonitor = "Lenovo ThinkVision T34w-20";

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

    [Fact] // REV-03
    public async Task The_confirmation_popup_is_laid_out_rather_than_left_to_the_browser()
    {
        await AssignADeskAndAChairAsync();
        await GotoAsync("/review");

        // The button waits for an address now, and these two are about what happens after it: the
        // confirmation dialog and the phrase in it.
        await FillAddressAsync();

        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Rent This Setup" }).ClickAsync();
        await Expect(Page.Locator("dialog[open]")).ToBeVisibleAsync();

        // The panel is a surface with room in it. It had none: the padding lived in a scoped
        // stylesheet that this application never links.
        var panel = Page.Locator("dialog[open] .modal__panel");
        var padding = await panel.EvaluateAsync<double>("panel => parseFloat(getComputedStyle(panel).paddingTop)");
        padding.Should().BeGreaterThan(8, "the panel has space inside it, rather than the content touching its edges");

        // Two controls, on one row, apart from each other and the same height.
        var actions = Page.Locator("dialog[open] .modal__actions");
        (await actions.EvaluateAsync<string>("actions => getComputedStyle(actions).display")).Should().Be("flex");
        (await actions.EvaluateAsync<double>("actions => parseFloat(getComputedStyle(actions).columnGap)"))
            .Should().BeGreaterThan(0, "the two controls are separated rather than touching");

        var heights = await actions.Locator("button").EvaluateAllAsync<double[]>("buttons => buttons.map(b => Math.round(b.getBoundingClientRect().height))");
        heights.Should().HaveCount(2);
        heights[0].Should().Be(heights[1], "controls on one row line up");

        // And the panel is a readable column rather than a band across the window.
        var dialog = Page.Locator("dialog[open]");
        (await dialog.EvaluateAsync<double>("d => d.getBoundingClientRect().width")).Should().BeLessThan(600);
        (await dialog.EvaluateAsync<double>("d => parseFloat(getComputedStyle(d).borderTopLeftRadius)"))
            .Should().BeGreaterThan(0, "the panel has corners");
    }

    [Fact] // REV-04
    public async Task Only_the_phrase_to_type_is_emphasised_in_the_confirmation()
    {
        await AssignADeskAndAChairAsync();
        await GotoAsync("/review");

        // The button waits for an address now, and these two are about what happens after it: the
        // confirmation dialog and the phrase in it.
        await FillAddressAsync();

        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Rent This Setup" }).ClickAsync();
        await Expect(Page.Locator("dialog[open]")).ToBeVisibleAsync();

        var weights = await Page.EvaluateAsync<double[]>(@"() => {
          const field = document.querySelector('#demo-confirmation').closest('.field');
          const prompt = field.querySelector('.field__prompt');
          const phrase = prompt.querySelector('strong');
          const weight = element => parseInt(getComputedStyle(element).fontWeight, 10);
          return [weight(prompt), weight(phrase)];
        }");

        // "Type ... to confirm" is the instruction; the phrase is the thing to type. The label's own
        // weight made both bold, so the customer could not tell which words to copy - and the words to
        // copy were the whole line.
        weights[0].Should().Be(400, "the words around the phrase are regular");
        weights[1].Should().BeGreaterThanOrEqualTo(700, "and the phrase itself is what stands out");
    }

    [Fact] // REV-05
    public async Task The_way_to_rent_waits_for_a_delivery_address()
    {
        await AssignADeskAndAChairAsync();
        await GotoAsync("/review");

        var rent = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Rent This Setup" });

        // The workspace is rentable - a desk and a chair - so what is missing is the address, and the
        // button says so rather than letting the customer find out at the last step.
        await Expect(rent).ToBeDisabledAsync();
        await Expect(rent).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(rent).ToHaveAttributeAsync("title", "Enter a delivery address before you rent.");

        // Typed the way a customer types it, and then reached for: no blur has happened, so nothing is
        // stored yet, and the button is open all the same. Waiting for the blur would be waiting for a
        // click that cannot happen, because the button the customer is aiming at is the disabled one.
        await Page.Locator("#delivery-address").PressSequentiallyAsync("Villa Lotus, Canggu");
        await Expect(rent).ToBeEnabledAsync();
        await Expect(rent).ToHaveAttributeAsync("aria-disabled", "false");

        // And it stops claiming the address is missing, which an enabled button must not say.
        await Expect(rent).Not.ToHaveAttributeAsync("title", "Enter a delivery address before you rent.");

        // One click: it saves what is in the field and opens the confirmation.
        await rent.ClickAsync();
        await Expect(Page.Locator("dialog[open]")).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Escape");

        // And what was typed is what was stored, which is why the click saves it.
        await Page.ReloadAsync();
        await Expect(Page.Locator("#delivery-address")).ToHaveValueAsync("Villa Lotus, Canggu");
    }
}
