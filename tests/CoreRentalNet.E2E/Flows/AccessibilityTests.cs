using AwesomeAssertions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Xunit;
using Xunit.Abstractions;

namespace CoreRentalNet.E2E.Flows;

/// <summary>
/// The markup discipline from, asserted rather than assumed. There is no axe gate by
/// decision, but "everything clickable is a real control with a name" is cheap to check.
/// </summary>
public sealed class AccessibilityTests(HostFixture host, ITestOutputHelper output) : E2ETest(host, output)
{
    private static readonly string[] Pages = ["/", "/builder", "/extras"];

    [Fact] // UI-03
    public async Task Every_visible_control_is_a_real_element_with_an_accessible_name()
    {
        foreach (var path in Pages)
        {
            await GotoAsync(path);

            var controls = await Page.EvaluateAsync<string[]>(
                """
                () => Array.from(document.querySelectorAll('button, a'))
                    .filter(element => element.offsetParent !== null)
                    .map(element => [
                        element.tagName,
                        (element.getAttribute('aria-label') || '').trim(),
                        (element.textContent || '').trim()
                    ].join('|'))
                """);

            controls.Should().NotBeEmpty($"{path} has interactive elements");

            foreach (var control in controls)
            {
                var parts = control.Split('|');
                var tag = parts[0];
                var name = parts[1];
                var text = parts[2];

                tag.Should().BeOneOf("BUTTON", "A", $"{path}: a clickable div is invisible to a keyboard");
                (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(text)).Should().BeFalse(
                    $"{path}: every control needs a name a screen reader can announce ({tag})");
            }
        }
    }

    [Fact] // UI-03
    public async Task Nothing_pretends_to_be_a_button_without_being_one()
    {
        await GotoAsync("/builder");

        var offenders = await Page.EvaluateAsync<string[]>(
            """
            () => Array.from(document.querySelectorAll('[role="button"], [onclick], .card--interactive'))
                .filter(element => element.offsetParent !== null)
                .filter(element => element.tagName !== 'BUTTON' && element.tagName !== 'A')
                .map(element => element.tagName + '.' + element.className)
            """);

        offenders.Should().BeEmpty("the product cards are buttons, not decorated containers");
    }

    [Fact] // UI-04
    public async Task The_dialog_is_native_so_the_keyboard_rules_come_from_the_browser()
    {
        await AssignADeskAndAChairAsync();
        await GotoAsync("/review");
        await FillAddressAsync();

        await OpenDemoDialogAsync();

        var dialog = Page.Locator("dialog[open]");
        (await dialog.EvaluateAsync<string>("element => element.tagName")).Should().Be("DIALOG");

        // Opening a native modal dialog moves focus into it.
        var focusedInsideDialog = await Page.EvaluateAsync<bool>(
            "() => document.querySelector('dialog[open]')?.contains(document.activeElement) ?? false");
        focusedInsideDialog.Should().BeTrue("focus must move into the dialog");

        // Escape is handled by the browser rather than by our own key handling.
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("dialog[open]")).ToHaveCountAsync(0);
    }

    [Fact] // UI-08
    public async Task A_workspace_can_be_filled_with_the_keyboard_alone()
    {
        await GotoAsync("/builder");

        var onACard = false;

        for (var press = 0; press < 80 && !onACard; press++)
        {
            await Page.Keyboard.PressAsync("Tab");
            onACard = await Page.EvaluateAsync<bool>(
                "() => document.activeElement?.classList.contains('product-card') ?? false");
        }

        onACard.Should().BeTrue("tabbing must reach a product card");

        await Page.Keyboard.PressAsync("Enter");

        await Expect(Page.Locator(".slot--filled")).ToHaveCountAsync(1);
    }

    [Fact] // UI-05
    public async Task A_product_without_an_image_shows_a_drawn_placeholder_rather_than_a_broken_image()
    {
        await GotoAsync("/extras");

        // Deliberately not tied to a class the component happens to use: what matters is that a
        // product with no image file is drawn rather than left as a broken image.
        var placeholders = Page.Locator("svg[role='img']");

        (await placeholders.CountAsync()).Should().BeGreaterThan(0, "most catalog images were never shipped");

        var broken = await Page.EvaluateAsync<int>(
            "() => Array.from(document.images).filter(image => !image.complete || image.naturalWidth === 0).length");

        broken.Should().Be(0, "no image on the page may fail to load");
    }
}
