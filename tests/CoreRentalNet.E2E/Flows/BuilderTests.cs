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

        await Page.Locator("button.product-card").First.ClickAsync();

        await Expect(Page.Locator(".slot--chair.slot--filled")).ToHaveCountAsync(1);
        await Expect(Page.Locator(".total-bar")).Not.ToContainTextAsync("Rp0");
    }

    [Fact] // WS-02
    public async Task Choosing_another_chair_replaces_the_first_one()
    {
        await GotoAsync("/builder");
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

    [Fact] // WS-10
    public async Task The_stepper_stops_at_the_capacity_the_table_declares()
    {
        await GotoAsync("/builder");
        await OpenSlotPickerAsync(".slot--monitor");
        await PickFirstCandidateAsync();
        await Expect(Page.Locator(".slot--monitor.slot--filled")).ToHaveCountAsync(1);

        var plus = Page.Locator(".slot--monitor .stepper__button").Nth(1);
        var minus = Page.Locator(".slot--monitor .stepper__button").Nth(0);

        await plus.ClickAsync();
        await plus.ClickAsync();
        await Expect(Page.Locator(".slot--monitor .slot__quantity")).ToHaveTextAsync("×3");

        await Expect(plus).ToBeDisabledAsync();
        await Expect(minus).ToBeEnabledAsync();
    }

    [Fact] // WS-04, WS-06
    public async Task Adding_a_monitor_from_the_panel_when_the_slot_is_full_is_refused_visibly()
    {
        await GotoAsync("/builder");
        await OpenSlotPickerAsync(".slot--monitor");
        await PickFirstCandidateAsync();

        var plus = Page.Locator(".slot--monitor .stepper__button").Nth(1);
        await plus.ClickAsync();
        await plus.ClickAsync();
        await Expect(plus).ToBeDisabledAsync();

        // The panel is still offering monitors, so the refusal has to be visible.
        await Page.GetByRole(AriaRole.Tab, new PageGetByRoleOptions { Name = "Accessories" }).ClickAsync();
        var monitorCard = Page.Locator("button.product-card", new PageLocatorOptions { HasTextString = "Ultrawide" });
        await Expect(monitorCard).ToBeVisibleAsync();
        await monitorCard.ClickAsync();

        await Expect(Page.Locator(".alert")).ToContainTextAsync("at most 3");
        await Expect(Page.Locator(".slot--monitor .slot__quantity")).ToHaveTextAsync("×3");
    }

    [Fact] // WS-08
    public async Task The_remove_control_empties_the_slot()
    {
        await AssignFirstProductAsync();

        await Page.Locator(".slot--chair .slot__remove").ClickAsync();

        await Expect(Page.Locator(".slot--chair.slot--filled")).ToHaveCountAsync(0);
        await Expect(Page.Locator(".slot--chair.slot--empty")).ToHaveCountAsync(1);
        await Expect(Page.Locator(".total-bar")).ToContainTextAsync("Rp0");
    }

    [Fact] // WS-09
    public async Task The_stepper_minus_is_bounded_at_one_because_removal_has_its_own_control()
    {
        await GotoAsync("/builder");
        await OpenSlotPickerAsync(".slot--monitor");
        await PickFirstCandidateAsync();
        await Expect(Page.Locator(".slot--monitor.slot--filled")).ToHaveCountAsync(1);

        var minus = Page.Locator(".slot--monitor .stepper__button").Nth(0);

        await Expect(minus).ToBeDisabledAsync();

        // Emptying the slot is the multiply control's job, not a side effect of the minus.
        await Page.Locator(".slot--monitor .slot__remove").ClickAsync();
        await Expect(Page.Locator(".slot--monitor.slot--empty")).ToHaveCountAsync(1);
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
        await Page.Locator("button.product-card").First.ClickAsync();
        var desks = Page.GetByRole(AriaRole.Tab, new PageGetByRoleOptions { Name = "Desks" });
        await desks.ClickAsync();
        await Page.Locator("button.product-card").First.ClickAsync();

        await OpenSlotPickerAsync(".slot--monitor");
        await PickFirstCandidateAsync();
        var plus = Page.Locator(".slot--monitor .stepper__button").Nth(1);
        await plus.ClickAsync();
        await plus.ClickAsync();

        await Expect(Page.Locator(".slot--filled")).ToHaveCountAsync(3);
        await Expect(Page.Locator(".slot--monitor .slot__quantity")).ToHaveTextAsync("×3");
    }
}
