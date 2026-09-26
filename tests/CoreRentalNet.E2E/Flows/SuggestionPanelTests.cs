using AwesomeAssertions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Xunit;
using Xunit.Abstractions;

namespace CoreRentalNet.E2E.Flows;

/// <summary>
/// Where the section sits, what it looks like before and after a run, and what a screen reader hears while
/// one is going.
/// </summary>
/// <remarks>
/// Same rule as the streaming tests: nothing here is paced by the fixture, so a run's duration is the
/// application's. That decides what can be asserted - see the note on AIWB-45 below, which is half covered
/// for exactly that reason and is recorded rather than papered over.
/// </remarks>
public sealed class SuggestionPanelTests : AuthenticatedE2ETest
{
    private readonly ITestOutputHelper output;

    public SuggestionPanelTests(HostFixture host, ITestOutputHelper output)
        : base(host, output)
    {
        this.output = output;
    }

    [Fact] // AIWB-44
    public async Task The_section_sits_above_the_canvas_and_is_one_line_when_idle()
    {
        await BeginAsync("suggested");

        // Above the canvas: it rewrites the region it sits beside, so it belongs next to what it changes
        // rather than in the product panel, which on a phone becomes a row of chips.
        await Expect(Page.Locator(".workspace-stage")).ToBeVisibleAsync();

        var section = await Page.Locator("[data-testid='suggestion-panel']").BoundingBoxAsync();
        var canvas = await Page.Locator(".workspace-stage").BoundingBoxAsync();

        section.Should().NotBeNull();
        canvas.Should().NotBeNull();
        section!.Y.Should().BeLessThan(canvas!.Y, "the section sits above the canvas");

        // The field and the control, and nothing else. What is absent while idle is the prose around them,
        // because an offer of help that fills the page before it is used is a section a customer scrolls past.
        await Expect(Page.Locator("[data-testid='suggestion-panel']")).ToHaveAttributeAsync("data-expanded", "false");
        await Expect(Page.Locator("[data-testid='suggestion-query']")).ToBeVisibleAsync();
        await Expect(Page.Locator("[data-testid='suggestion-submit']")).ToBeVisibleAsync();
        await Expect(Page.Locator("[data-testid='suggestion-panel'] h2")).ToHaveCountAsync(0);
    }

    [Fact] // AIWB-45, the half an immediate run can reach
    public async Task The_section_is_expanded_once_there_is_something_to_show()
    {
        await BeginAsync("suggested");

        await Page.Locator("[data-testid='suggestion-query']").FillAsync("a desk and a chair");
        await Page.Locator("[data-testid='suggestion-submit']").ClickAsync();

        await Expect(Page.Locator("[data-testid='suggestion-candidates']")).ToBeVisibleAsync();

        // Expanded while candidates are shown, and the prose is back with them.
        await Expect(Page.Locator("[data-testid='suggestion-panel']")).ToHaveAttributeAsync("data-expanded", "true");
        await Expect(Page.Locator("[data-testid='suggestion-panel'] h2")).ToHaveCountAsync(1);

        // NOT asserted, and it cannot be against a run that finishes immediately: "expands WHILE A RUN IS IN
        // FLIGHT". The section is driven into that state for a few milliseconds of real network time, so a
        // test that looked for it would be timing-dependent - and Stop is offered for the same fleeting
        // window, which is why AIWB-27, 28 and 49 are uncovered too. Recorded in the capsule.
    }

    [Fact] // AIWB-46
    public async Task On_a_phone_the_section_is_a_full_width_card_above_the_canvas()
    {
        await Page.SetViewportSizeAsync(390, 844);

        await BeginAsync("suggested", phone: true);
        await Expect(Page.Locator(".workspace-stage")).ToBeVisibleAsync();

        var section = await Page.Locator("[data-testid='suggestion-panel']").BoundingBoxAsync();
        var canvas = await Page.Locator(".workspace-stage").BoundingBoxAsync();

        section.Should().NotBeNull();
        canvas.Should().NotBeNull();

        // Full width: the card takes the column rather than sharing the row with anything, which is what it
        // has to do where the product panel has become a row of chips.
        section!.Width.Should().BeGreaterThan(
            canvas!.Width * 0.9f,
            "on a phone the section is a card across the column, not a panel beside it");

        // And it spans the SAME column the canvas does rather than sitting in one of its own: two block
        // elements in one column would be the same width by accident, so the left edge is what says they are
        // in the same column rather than in two.
        Math.Abs(section.X - canvas.X).Should().BeLessThan(
            2f,
            "the section and the canvas share the column, so they share its left edge");

        section.Y.Should().BeLessThan(canvas.Y, "and it stays above the canvas");
    }

    [Fact] // AIWB-52
    public async Task The_prose_is_not_a_live_region_and_the_progress_and_outcome_are()
    {
        await BeginAsync("suggested");

        // The announcer is in the document BEFORE anything happens. A live region added to the page at the
        // same moment as its first content is not reliably announced: the browser has to be observing it
        // already, and an element that arrives full never changes at all.
        var announcer = Page.Locator("[data-testid='suggestion-announcement']");

        await Expect(announcer).ToHaveAttributeAsync("role", "status");
        await Expect(announcer).ToHaveTextAsync(string.Empty, new LocatorAssertionsToHaveTextOptions { IgnoreCase = false });

        await Page.Locator("[data-testid='suggestion-query']").FillAsync("a desk and a chair");
        await Page.Locator("[data-testid='suggestion-submit']").ClickAsync();

        await Expect(Page.Locator("[data-testid='suggestion-candidates']")).ToBeVisibleAsync();

        // Progress and the outcome, and never the model's prose: the stage lines are the application's own
        // short sentences, and the candidates arriving is the terminal outcome a screen reader hears.
        var announced = await announcer.InnerTextAsync();

        output.WriteLine($"announced: {announced}");
        announced.Should().Contain("ready", "the outcome of a successful run is announced");

        // And the prose is outside any live region, so a run does not narrate itself field by field.
        var prose = Page.Locator("[data-testid='suggestion-stream']");

        await Expect(prose).ToBeVisibleAsync();
        await Expect(prose).Not.ToHaveAttributeAsync("role", "status");
        await Expect(prose).Not.ToHaveAttributeAsync("aria-live", "polite");
        await Expect(prose).Not.ToHaveAttributeAsync("aria-live", "assertive");

        // Nothing inside it is a live region either, which is the version of this mistake that is easy to make
        // by adding a role to the piece that changes.
        await Expect(Page.Locator("[data-testid='suggestion-stream'] [aria-live]")).ToHaveCountAsync(0);
        await Expect(Page.Locator("[data-testid='suggestion-stream'] [role='status']")).ToHaveCountAsync(0);
    }

    [Fact] // task 5's browser half of AIWB-20 and AIWB-21, whose policy is asserted at unit level
    public async Task The_section_is_absent_for_a_customer_without_the_permission()
    {
        // "Sari Builder" carries the catalogue's permission and not the AI one, so it may use the builder
        // page and must not be offered the section. Hiding is presentation rather than authorisation - the
        // endpoint refuses the same account, which SuggestionEndpointTests asserts - but a customer who never
        // sees the section is what a closed feature looks like from the outside, and the page has to look
        // exactly as it does today.
        await Host.ChooseScenarioAsync("suggested");
        await SignInAsync("Sari Builder");
        await WaitForCircuitAsync(".workspace-stage");

        await Expect(Page.Locator("[data-testid='suggestion-panel']")).ToHaveCountAsync(0);

        // And the page really is the builder, so the assertion above is about the section rather than about
        // having landed somewhere else.
        await Expect(Page.Locator(".workspace-stage")).ToBeVisibleAsync();
    }

    private async Task BeginAsync(string scenario, bool phone = false)
    {
        await Host.ChooseScenarioAsync(scenario);

        // "Dewi Reader" holds the AI permission.
        await SignInAsync("Dewi Reader");
        await WaitForCircuitAsync(".workspace-stage");

        // The canvas only exists once something is in it, and the way in differs by width: the product panel
        // is not on screen on a phone, so there the empty slot itself is the way in.
        if (phone)
        {
            await OpenSlotPickerAsync(".slot--desk");
            await PickFirstCandidateAsync();
        }
        else
        {
            await AssignFirstProductAsync("Desks");
        }

        await Expect(Page.Locator("[data-testid='suggestion-panel']")).ToBeVisibleAsync();
    }
}
