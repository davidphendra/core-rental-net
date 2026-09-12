using AwesomeAssertions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Xunit;
using Xunit.Abstractions;

namespace CoreRentalNet.E2E.Flows;

/// <summary>
/// The canvas: what a customer can and cannot put in a workspace, driven the way they drive it.
/// </summary>
public sealed class BuilderTests(HostFixture host, ITestOutputHelper output) : E2ETest(host, output)
{
    [Fact] // WS-01
    public async Task Clicking_a_product_card_fills_its_slot_without_choosing_a_slot()
    {
        await GotoAsync("/builder");
        await Expect(Page.Locator(".workspace-stage")).ToBeVisibleAsync();

        await ChooseCategoryAsync("Chairs");
        await Page.Locator("button.product-card").First.ClickAsync();

        await Expect(Page.Locator(".slot--chair.slot--filled")).ToHaveCountAsync(1);
        await Expect(Page.Locator(".total-bar")).Not.ToContainTextAsync("Rp0");
    }

    [Fact] // WS-02
    public async Task Choosing_another_chair_replaces_the_first_one()
    {
        await GotoAsync("/builder");
        await ChooseCategoryAsync("Chairs");
        var cards = Page.Locator("button.product-card");

        await cards.First.ClickAsync();
        await Expect(Page.Locator(".slot--chair.slot--filled")).ToHaveCountAsync(1);
        var first = await Page.Locator(".slot--chair").InnerTextAsync();

        await cards.Nth(1).ClickAsync();

        // A count of one is true before and after a replacement, so it is not a place to wait.
        // Waiting for the text to change is.
        await Expect(Page.Locator(".slot--chair")).Not.ToContainTextAsync("Seminyak Lounge");
        await Expect(Page.Locator(".slot--filled")).ToHaveCountAsync(1);

        var second = await Page.Locator(".slot--chair").InnerTextAsync();
        second.Should().NotBe(first, "a single-capacity slot is replaced, not stacked");
    }

    [Fact] // WS-05
    public async Task Clicking_an_empty_slot_opens_a_picker_already_filtered_to_it()
    {
        await GotoAsync("/builder");

        await OpenSlotPickerAsync(".slot--monitor");

        var candidates = Page.Locator("dialog[open] .picker button.product-card");
        var count = await candidates.CountAsync();
        count.Should().BeGreaterThan(0);

        var text = await Page.Locator("dialog[open] .picker").InnerTextAsync();
        text.Should().NotContain("Lounge", "a chair cannot go in the monitor slot");
        text.Should().NotContain("Lamp");
        text.Should().NotContain("Plant");
    }

    [Fact] // WS-05
    public async Task Picking_from_that_dialog_fills_the_slot_it_was_opened_from()
    {
        await GotoAsync("/builder");
        await OpenSlotPickerAsync(".slot--monitor");

        await PickFirstCandidateAsync();

        await Expect(Page.Locator("dialog[open]")).ToHaveCountAsync(0);
        await Expect(Page.Locator(".slot--monitor.slot--filled")).ToHaveCountAsync(1);
        await Expect(Page.Locator(".slot--chair.slot--filled")).ToHaveCountAsync(0);
    }

    [Fact] // WS-07
    public async Task The_monitor_row_offers_exactly_the_places_the_table_declares()
    {
        await GotoAsync("/builder");

        // Three places, from the slot table. A filled place stops inviting, so the row shows what is
        // still free without the customer having to count.
        await Expect(Page.Locator(".slot--monitor.slot--empty")).ToHaveCountAsync(3);

        for (var place = 0; place < 3; place++)
        {
            await OpenSlotPickerAsync(".slot--monitor");
            await PickCandidateAsync(place);

            await Expect(Page.Locator(".slot--monitor.slot--filled")).ToHaveCountAsync(place + 1);
        }

        await Expect(Page.Locator(".slot--monitor.slot--empty")).ToHaveCountAsync(0);
    }

    [Fact] // WS-08
    public async Task Adding_a_monitor_from_the_panel_when_the_slot_is_full_is_refused_visibly()
    {
        await GotoAsync("/builder");

        for (var place = 0; place < 3; place++)
        {
            await OpenSlotPickerAsync(".slot--monitor");
            await PickCandidateAsync(place);
        }

        await Expect(Page.Locator(".slot--monitor.slot--filled")).ToHaveCountAsync(3);

        // A fourth is refused, and the refusal is on the page rather than in a console. Every monitor
        // in the catalog is named in inches, so the quote in this one is escaped.
        await ChooseCategoryAsync("Accessories");
        await Page.Locator("aside button.product-card")
            .Filter(new LocatorFilterOptions { HasTextString = "Seminyak 32\" UHD" }).First.ClickAsync();

        await Expect(Page.Locator(".alert")).ToContainTextAsync("at most 3");
        await Expect(Page.Locator(".slot--monitor.slot--filled")).ToHaveCountAsync(3);
    }

    [Fact] // WS-08
    public async Task The_remove_control_empties_the_slot()
    {
        await AssignFirstProductAsync("Chairs");

        await Page.Locator(".slot--chair .slot__remove").ClickAsync();

        await Expect(Page.Locator(".slot--chair.slot--filled")).ToHaveCountAsync(0);
        await Expect(Page.Locator(".slot--chair.slot--empty")).ToHaveCountAsync(1);
        await Expect(Page.Locator(".total-bar")).ToContainTextAsync("Rp0");
    }

    [Fact] // WS-09
    public async Task Each_monitor_box_is_removed_on_its_own()
    {
        await GotoAsync("/builder");

        await OpenSlotPickerAsync(".slot--monitor");
        await PickFirstCandidateAsync();

        await Expect(Page.Locator(".slot--monitor.slot--filled")).ToHaveCountAsync(1);
        await Expect(Page.Locator(".slot--monitor.slot--empty")).ToHaveCountAsync(2);

        // Removing a box takes that one monitor away and leaves the row the size it was, so the
        // canvas does not move under the customer as monitors come and go.
        await Page.Locator(".slot--monitor.slot--filled .slot__remove").ClickAsync();

        await Expect(Page.Locator(".slot--monitor.slot--filled")).ToHaveCountAsync(0);
        await Expect(Page.Locator(".slot--monitor")).ToHaveCountAsync(3);
    }

    [Fact] // WS-11, WS-12
    public async Task The_running_total_follows_the_workspace()
    {
        await GotoAsync("/builder");
        await Expect(Page.Locator(".total-bar")).ToContainTextAsync("Rp0");

        await Page.Locator("button.product-card").First.ClickAsync();
        await Expect(Page.Locator(".total-bar")).Not.ToContainTextAsync("Rp0");

        var withOne = await Page.Locator(".total-bar__amount").InnerTextAsync();

        await Page.Locator("button.product-card").Nth(1).ClickAsync();
        await Expect(Page.Locator(".total-bar__amount")).Not.ToHaveTextAsync(withOne);
    }

    [Fact] // WS-03
    public async Task A_workspace_can_hold_a_desk_a_chair_and_three_monitors()
    {
        await GotoAsync("/builder");

        await ChooseCategoryAsync("Chairs");
        await Page.Locator("button.product-card").First.ClickAsync();

        await ChooseCategoryAsync("Desks");
        await Page.Locator("button.product-card").First.ClickAsync();

        // Three monitors, and not the same one three times: the slot holds products, not a count.
        for (var place = 0; place < 3; place++)
        {
            await OpenSlotPickerAsync(".slot--monitor");
            await PickCandidateAsync(place);
        }

        await Expect(Page.Locator(".slot--monitor.slot--filled")).ToHaveCountAsync(3);
        await Expect(Page.Locator(".slot--filled")).ToHaveCountAsync(5);
    }

    [Fact] // SLOT-14
    public async Task The_desk_can_be_clicked_where_the_desk_is()
    {
        await GotoAsync("/builder");

        // The chair stood over the middle of the desk's box, so this click used to land on the chair
        // and the desk could not be added at all. The picker is opened by clicking the desk's own
        // box, in its middle, with no position hint to work around anything.
        await OpenSlotPickerAsync(".slot--desk");
        await Expect(Page.Locator("dialog[open] button.product-card")).ToHaveCountAsync(10);

        await PickFirstCandidateAsync();

        await Expect(Page.Locator(".slot--desk.slot--filled")).ToHaveCountAsync(1);
        await Expect(Page.Locator(".slot--desk.slot--empty")).ToHaveCountAsync(0);
    }
}
