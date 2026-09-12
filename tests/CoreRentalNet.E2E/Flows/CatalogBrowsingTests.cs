using AwesomeAssertions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Xunit;
using Xunit.Abstractions;

namespace CoreRentalNet.E2E.Flows;

/// <summary>
/// Browsing the catalog: three categories rather than four, products told apart by subcategory, and
/// a search field on the panel and on the picker that belong to their own surface.
/// </summary>
public sealed class CatalogBrowsingTests(HostFixture host, ITestOutputHelper output) : E2ETest(host, output)
{
    private ILocator Panel => Page.Locator("aside[aria-label='Selection panel']");

    private ILocator PanelCards => Panel.Locator("button.product-card");

    private ILocator PickerCards => Page.Locator("dialog[open] button.product-card");

    [Fact] // CAT-40
    public async Task There_are_three_categories_and_no_extras_among_them()
    {
        await GotoAsync("/builder");

        var tabs = Panel.Locator("nav[aria-label='Product categories'] button");

        await Expect(tabs).ToHaveCountAsync(3);
        await Expect(Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Extras" })).ToHaveCountAsync(0);
        await Expect(Panel.Locator("nav[aria-label='Product categories'] [aria-label='Desks']")).ToHaveCountAsync(1);
        await Expect(Panel.Locator("nav[aria-label='Product categories'] [aria-label='Chairs']")).ToHaveCountAsync(1);
        await Expect(Panel.Locator("nav[aria-label='Product categories'] [aria-label='Accessories']")).ToHaveCountAsync(1);
    }

    [Fact] // CAT-41
    public async Task The_accessory_category_is_shown_under_a_heading_per_subcategory()
    {
        await GotoAsync("/builder");
        await ChooseCategoryAsync("Accessories");

        await Expect(PanelCards).ToHaveCountAsync(42);
        await Expect(Panel.Locator("h3")).ToHaveTextAsync(
            ["Monitors", "Lamps", "Plants", "Coffee Machines", "Bean bags"]);
    }

    [Fact] // CAT-42
    public async Task A_category_that_is_one_kind_of_thing_has_no_headings()
    {
        await GotoAsync("/builder");

        await Expect(PanelCards).ToHaveCountAsync(10);
        await Expect(Panel.Locator("h3")).ToHaveCountAsync(0);
    }

    [Fact] // CAT-43
    public async Task The_panels_search_narrows_the_tab_it_is_in()
    {
        await GotoAsync("/builder");
        await ChooseCategoryAsync("Accessories");
        await Expect(PanelCards).ToHaveCountAsync(42);

        // "lamp" is not in any product's name - the lamps are Pererenan Clip Lights - so this is the
        // kind of thing being matched as well as the name, which is what makes the field usable.
        await Page.Locator("#panel-search").FillAsync("lamp");

        await Expect(PanelCards).ToHaveCountAsync(6);
        await Expect(Panel.Locator("h3")).ToHaveTextAsync("Lamps");

        await Page.Locator("#panel-search").FillAsync("nothing like this");

        await Expect(PanelCards).ToHaveCountAsync(0);
        await Expect(Panel).ToContainTextAsync("Nothing here is named");
    }

    [Fact] // CAT-44
    public async Task Choosing_another_tab_empties_the_search()
    {
        await GotoAsync("/builder");
        await Page.Locator("#panel-search").FillAsync("lamp");

        await ChooseCategoryAsync("Chairs");

        await Expect(Page.Locator("#panel-search")).ToHaveValueAsync(string.Empty);
        await Expect(PanelCards).ToHaveCountAsync(10);
    }

    [Fact] // CAT-45
    public async Task The_picker_holds_only_the_kind_of_thing_that_was_clicked()
    {
        await GotoAsync("/builder");

        // One query for the slot's own subcategory: the lamp box offers the six lamps, not the whole
        // accessory category, and asks the catalog nothing about desks or chairs.
        await OpenSlotPickerAsync(".slot--lamp");

        await Expect(PickerCards).ToHaveCountAsync(6);
        await Expect(Page.Locator("dialog[open] h3")).ToHaveCountAsync(0);
    }

    [Fact] // CAT-46
    public async Task The_pickers_search_narrows_its_own_products_and_not_the_panels()
    {
        await GotoAsync("/builder");
        await ChooseCategoryAsync("Accessories");
        await Page.Locator("#panel-search").FillAsync("lamp");
        await Expect(PanelCards).ToHaveCountAsync(6);

        await OpenSlotPickerAsync(".slot--chair");

        // The picker opens with a search of its own, empty, showing everything the chair box holds.
        await Expect(Page.Locator("#picker-search")).ToHaveValueAsync(string.Empty);
        await Expect(PickerCards).ToHaveCountAsync(10);

        await Page.Locator("#picker-search").FillAsync("seminyak");

        // One chair is called Seminyak Lounge. Waiting on the count rather than reading it once:
        // the list is re-rendered by the same keystroke that narrows it.
        await Expect(PickerCards).ToHaveCountAsync(1);

        // And the panel is exactly where it was left.
        await Expect(Page.Locator("#panel-search")).ToHaveValueAsync("lamp");
        await Expect(PanelCards).ToHaveCountAsync(6);
    }

    [Fact] // CAT-47
    public async Task The_store_shows_the_merged_category_the_same_way()
    {
        await GotoAsync("/extras");
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Accessories" }).ClickAsync();

        await Expect(Page.Locator(".grid-store button.product-card")).ToHaveCountAsync(42);
        await Expect(Page.Locator("section:has(.grid-store) h2")).ToHaveTextAsync(
            ["Monitors", "Lamps", "Plants", "Coffee Machines", "Bean bags"]);
    }

    [Fact] // CAT-48
    public async Task The_panels_search_field_has_no_rule_above_it_and_room_below_it()
    {
        await GotoAsync("/builder");

        var measured = await Page.EvaluateAsync<double[]>(@"() => {
          const input = document.querySelector('#panel-search');
          const wrapper = input.parentElement.parentElement;
          const list = input.parentElement.nextElementSibling;
          return [parseFloat(getComputedStyle(wrapper).borderTopWidth),
                  Math.round(list.getBoundingClientRect().top - input.getBoundingClientRect().bottom)];
        }");

        measured[0].Should().Be(0, "nothing draws a line between the categories and the field");
        measured[1].Should().BeGreaterThanOrEqualTo(20, "and whatever the field draws inside it has room");
    }
}
