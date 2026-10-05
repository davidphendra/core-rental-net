using AwesomeAssertions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace CoreRentalNet.E2E.Flows;

/// <summary>
/// The run as a customer watches it: the application's own progress lines going active and complete, then the
/// answer.
/// </summary>
/// <remarks>
/// <para>
/// Every test drives the section through the page rather than through the endpoint, and the answer is chosen
/// by the test rather than guessed from the customer's words - so a test about a failure does not have to
/// phrase its way into one.
/// </para>
/// <para>
/// <b>Nothing here is paced by the fixture.</b> The stand-in answers immediately, so a run's duration is the
/// application's and the transport's. That is deliberate: a stand-in that waited between events made a run's
/// length a property of the test rather than of the code, and an assertion about what happens mid-run was
/// really an assertion about the wait.
/// </para>
/// <para>
/// <b>The model's own words are never on the page.</b> The application shows its own words and its own checked
/// values, so a leaky stand-in cannot put a price, a link or a product name in front of a customer.
/// </para>
/// <para>
/// <b>What that costs, stated rather than hidden.</b> Three scenarios in the matrix - AIWB-27 and AIWB-28
/// (cancelling is reported as stopped) and AIWB-49 (reloading mid-run cancels it) - need a run that is still
/// going when the browser acts. Against an immediate run the UI cannot be driven into that state at all: Stop
/// is offered only while a run is in flight, and the run is over before a click can land. They are therefore
/// NOT covered at this level, and this file does not pretend otherwise. The frame-level behaviour they rest on
/// is asserted in SuggestionEndpointTests and in the application's own unit tests, where a cancellation can be
/// raised deterministically instead of raced.
/// </para>
/// </remarks>
public sealed class SuggestionStreamTests : AuthenticatedE2ETest
{
    public SuggestionStreamTests(HostFixture host, ITestOutputHelper output)
        : base(host, output)
    {
    }

    [Fact] // AIWB-25
    public async Task The_answer_is_shown_and_the_model_s_own_words_are_not()
    {
        await BeginAsync("suggested");

        await AskAsync("a desk and a chair, for a small room");

        await Expect(Page.Locator("[data-testid='suggestion-candidates']")).ToBeVisibleAsync();

        // The model's own words are never rendered: a customer reads the application's checked values, never the
        // document the model answered in.
        await Expect(Page.Locator("[data-testid='suggestion-narrative']")).ToHaveCountAsync(0);
        await Expect(Page.Locator("[data-testid='suggestion-stream']")).ToHaveCountAsync(0);

        await Expect(Page.Locator("[data-testid='suggestion-option']")).ToHaveCountAsync(3);

        // Each candidate is drawn as its products - cards, not a list of names - so a candidate can be
        // judged at a glance rather than read line by line.
        await Expect(Page.Locator("[data-testid='suggestion-option']").First
            .Locator("[data-testid='suggestion-line']").First).ToBeVisibleAsync();

        // And the choice is a real primary button, one per candidate, not an underlined link.
        var apply = Page.Locator("button[data-testid='suggestion-apply']");
        await Expect(apply).ToHaveCountAsync(3);
        await Expect(apply.First).ToHaveClassAsync(new Regex("button--primary"));

        // And a candidate says what it is: an amount the application recomputed from the catalogue, so a shape
        // that failed to deserialize would leave the block empty and a field rendered wrong would leave it blank.
        await Expect(Page.Locator("[data-testid='suggestion-option']").First).ToContainTextAsync("/mo");
    }

    [Fact] // a second and third candidate sit beside the first rather than under it
    public async Task More_than_one_candidate_is_shown_side_by_side()
    {
        await BeginAsync("suggested");
        await AskAsync("a desk and a chair");

        await Expect(Page.Locator("[data-testid='suggestion-candidates']")).ToBeVisibleAsync();

        var first = await Page.Locator("[data-testid='suggestion-option']").Nth(0).BoundingBoxAsync();
        var second = await Page.Locator("[data-testid='suggestion-option']").Nth(1).BoundingBoxAsync();

        first.Should().NotBeNull();
        second.Should().NotBeNull();
        second!.X.Should().BeGreaterThan(first!.X, "a second candidate sits beside the first, not under it");
        Math.Abs(second.Y - first.Y).Should().BeLessThan(2f, "and on the same row");
    }

    [Fact] // AIWB-26
    public async Task A_leaky_model_answer_is_never_shown()
    {
        await BeginAsync("leaky");
        await AskAsync("a desk and a chair");

        await Expect(Page.Locator("[data-testid='suggestion-candidates']")).ToBeVisibleAsync();

        var panel = await Page.Locator("[data-testid='suggestion-panel']").InnerTextAsync();

        // The stand-in was told to leak a price, a link and a rationale; none of it can reach the page because
        // the model's words are not rendered at all.
        panel.Should().NotContain("Rp", "a price on the page would make the run an offer");
        panel.Should().NotContain("1.206.000");
        panel.Should().NotContain("example.invalid", "nor a link out of the page");
        panel.Should().NotContain("An uncluttered setup", "nor the model's rationale");
    }

    [Fact] // AIWB-29
    public async Task A_failed_run_offers_another_go()
    {
        // The scenario that stops mid-answer: the result never closes, so the run ends as a failure.
        await BeginAsync("truncated");
        await AskAsync("a desk and a chair");

        await Expect(Page.Locator("[data-testid='suggestion-failed']")).ToBeVisibleAsync();
        await Expect(Page.Locator("[data-testid='suggestion-retry']")).ToBeVisibleAsync();
    }

    [Fact] // AIWB-30
    public async Task The_stage_list_folds_away_and_back_and_every_finished_node_is_checked()
    {
        await BeginAsync("suggested");
        await AskAsync("a desk and a chair");

        await Expect(Page.Locator("[data-testid='suggestion-candidates']")).ToBeVisibleAsync();

        // Retained and collapsed once the run is over: the one-liner is there, the list is not.
        await Expect(Page.Locator("[data-testid='suggestion-stages']")).ToHaveCountAsync(0);
        var toggle = Page.Locator("[data-testid='suggestion-stages-toggle']");
        await Expect(toggle).ToBeVisibleAsync();
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "false");

        // Opening it keeps the toggle, so the list it opened can be folded away again.
        await toggle.ClickAsync();
        await Expect(toggle).ToBeVisibleAsync();
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Page.Locator("[data-testid='suggestion-stages']")).ToBeVisibleAsync();
        await Expect(Page.Locator("[data-testid='suggestion-stages']")).ToContainTextAsync("Reading your request");

        // Every stage the run finished is checked, and none is left spinning once the run is over.
        await Expect(Page.Locator("[data-testid='suggestion-stage-done']").First).ToBeVisibleAsync();
        await Expect(Page.Locator("[data-testid='suggestion-stage-spinner']")).ToHaveCountAsync(0);
        await Expect(Page.Locator("[data-testid='suggestion-stages'] li[data-stage-state='active']"))
            .ToHaveCountAsync(0);

        // And clicking it again folds the list away, with the toggle still there to open it once more.
        await toggle.ClickAsync();
        await Expect(Page.Locator("[data-testid='suggestion-stages']")).ToHaveCountAsync(0);
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "false");
    }

    private async Task BeginAsync(string scenario)
    {
        await Host.ChooseScenarioAsync(scenario);

        // "Dewi Reader" is the account that holds the AI permission. One without it never sees the section at
        // all, which is what a closed feature looks like from the outside.
        await SignInAsync("Dewi Reader");
        await WaitForCircuitAsync(".workspace-stage");

        await Expect(Page.Locator("[data-testid='suggestion-panel']")).ToBeVisibleAsync();
    }

    private async Task AskAsync(string query)
    {
        await Page.Locator("[data-testid='suggestion-query']").FillAsync(query);
        await Page.Locator("[data-testid='suggestion-submit']").ClickAsync();
    }
}
