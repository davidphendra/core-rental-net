using AwesomeAssertions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Xunit;
using Xunit.Abstractions;

namespace CoreRentalNet.E2E.Flows;

/// <summary>How the workspace itself is presented, rather than what it can do.</summary>
public sealed class WorkspacePresentationTests(HostFixture host, ITestOutputHelper output) : E2ETest(host, output)
{
    [Fact] // SLOT-04
    public async Task Only_the_zones_the_catalog_can_fill_are_shown()
    {
        await GotoAsync("/builder");

        await Expect(Page.Locator(".zone-list .slot--coffee-station")).ToHaveCountAsync(1);
        await Expect(Page.Locator(".zone-list .slot--relax-zone")).ToHaveCountAsync(1);

        // Garage and Outdoor Gear were removed with the partner item; a zone no product can fill
        // must not be drawn.
        await Expect(Page.Locator(".zone-list .slot--garage")).ToHaveCountAsync(0);
        await Expect(Page.Locator(".zone-list .slot--outdoor-gear")).ToHaveCountAsync(0);
        await Expect(Page.Locator(".zone-list section")).ToHaveCountAsync(2);
    }

    [Fact] // SLOT-01
    public async Task The_canvas_shows_every_slot_the_table_declares()
    {
        await GotoAsync("/builder");

        foreach (var slot in new[] { "desk", "chair", "monitor", "lamp", "plant" })
        {
            await Expect(Page.Locator($".scene .slot--{slot}")).ToHaveCountAsync(1);
        }

        await Expect(Page.Locator(".scene .slot")).ToHaveCountAsync(5);
    }

    [Fact] // ADDR-06
    public async Task The_delivery_address_is_a_two_line_field()
    {
        await AssignFirstProductAsync();
        await GotoAsync("/review");

        var field = Page.Locator("#delivery-address");

        (await field.EvaluateAsync<string>("element => element.tagName")).Should().Be("TEXTAREA");
        await Expect(field).ToHaveAttributeAsync("rows", "2");
        await Expect(field).ToHaveAttributeAsync("maxlength", "200");
    }

    [Fact] // NAV-05
    public async Task A_disabled_control_says_so_and_cannot_be_reached_by_tab()
    {
        await GotoAsync("/builder");

        // This used to be the panel's link to the summary. The panel no longer has one, so the
        // control that has to shut while the workspace is empty is the floating bar's.
        var ready = Page.Locator(".total-bar button");

        await Expect(ready).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(ready).ToHaveAttributeAsync("title", "Add an item to your workspace first");

        // And it is checked by tabbing, not by reading tabindex: whether a disabled control can be
        // reached is the browser's behaviour, and an attribute is only a claim about it.
        var visited = new List<string>();
        var reached = false;

        for (var press = 0; press < 80; press++)
        {
            await Page.Keyboard.PressAsync("Tab");

            var landed = await Page.EvaluateAsync<string>(
                "() => { const el = document.activeElement; if (!el || el === document.body) return 'body';" +
                " const name = typeof el.className === 'string' ? el.className : '';" +
                " return el.tagName.toLowerCase() + '.' + name; }");

            if (landed.Contains("button--cta", StringComparison.Ordinal))
            {
                reached = true;
                break;
            }

            if (landed == "body")
            {
                break;
            }

            if (!visited.Contains(landed))
            {
                visited.Add(landed);
            }
        }

        reached.Should().BeFalse("a disabled control must be somewhere the keyboard cannot land");
        visited.Should().NotBeEmpty("the page must still have things to tab through, or this proves nothing");
    }
}
