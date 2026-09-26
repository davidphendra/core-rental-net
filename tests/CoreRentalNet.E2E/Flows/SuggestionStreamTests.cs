using AwesomeAssertions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Xunit;
using Xunit.Abstractions;

namespace CoreRentalNet.E2E.Flows;

/// <summary>
/// The run as a customer watches it: the model's words shown as prose, the answer after them, and what is left
/// on screen when a run fails.
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
/// <b>What that costs, stated rather than hidden.</b> Three scenarios in the matrix - AIWB-27 and AIWB-28
/// (cancelling is reported as stopped, and leaves the text in place) and AIWB-49 (reloading mid-run cancels
/// it) - need a run that is still going when the browser acts. Against an immediate run the UI cannot be
/// driven into that state at all: Stop is offered only while a run is in flight, and the run is over before a
/// click can land. They are therefore NOT covered at this level, and this file does not pretend otherwise.
/// The frame-level behaviour they rest on - that the application writes its stages, then the model's words,
/// then the answer, and that a cancelled request ends the run without a failure - is asserted in
/// SuggestionEndpointTests and in the application's own unit tests, where a cancellation can be raised
/// deterministically instead of raced.
/// </para>
/// </remarks>
public sealed class SuggestionStreamTests : AuthenticatedE2ETest
{
    private readonly ITestOutputHelper output;

    public SuggestionStreamTests(HostFixture host, ITestOutputHelper output)
        : base(host, output)
    {
        this.output = output;
    }

    [Fact] // AIWB-25
    public async Task The_model_s_words_are_shown_as_prose_and_the_candidates_come_after_them()
    {
        await BeginAsync("suggested");

        await AskAsync("a desk and a chair, for a small room");

        await Expect(Page.Locator("[data-testid='suggestion-candidates']")).ToBeVisibleAsync();

        var streamed = await Page.Locator("[data-testid='suggestion-stream']").InnerTextAsync();

        // The model's words as prose. A customer reads a description of an idea, never the document it
        // arrived in.
        streamed.Should().Contain("An uncluttered setup for one person");
        streamed.Should().NotContain("{");
        streamed.Should().NotContain("\"");
        streamed.Should().NotContain("sku");

        // And the words stay where they are once the answer arrives: a run is a sequence, not a replacement.
        var words = await Page.Locator("[data-testid='suggestion-stream']").BoundingBoxAsync();
        var candidates = await Page.Locator("[data-testid='suggestion-candidates']").BoundingBoxAsync();

        words.Should().NotBeNull();
        candidates.Should().NotBeNull();
        words!.Y.Should().BeLessThan(candidates!.Y, "the model's words come first, then the candidates");

        await Expect(Page.Locator("[data-testid='suggestion-option']")).ToHaveCountAsync(3);

        // And a candidate says what it is: a label the application assigned by rank and an amount it recomputed
        // from the catalogue. Both arrive in the frame, so a shape that failed to deserialize would leave the
        // block empty - but a field rendered wrong would leave it blank, which this catches.
        await Expect(Page.Locator("[data-testid='suggestion-option']").First).ToContainTextAsync("Budget");
        await Expect(Page.Locator("[data-testid='suggestion-option']").First).ToContainTextAsync("/mo");
    }

    [Fact] // AIWB-26
    public async Task Streamed_text_carries_no_price_and_no_product_name()
    {
        await BeginAsync("leaky");
        await AskAsync("a desk and a chair");

        await Expect(Page.Locator("[data-testid='suggestion-candidates']")).ToBeVisibleAsync();

        var streamed = await Page.Locator("[data-testid='suggestion-stream']").InnerTextAsync();

        output.WriteLine(streamed);

        // The stand-in was told to leak both, so this is not a fixture that happens to be clean: the amount
        // is what the application's strip removes before the customer reads it.
        streamed.Should().NotContain("Rp", "a price in streamed text would make the run an offer");
        streamed.Should().NotContain("1.206.000");
        streamed.Should().NotContain("example.invalid", "nor a link out of the page");

        // And the two assertions above only mean something because the text arrived with the leak removed
        // FROM THE MIDDLE OF A SENTENCE - not because the sentence never mentioned either.
        streamed.Should().Contain("An uncluttered setup for", "the rationale arrived");
        streamed.Should().Contain("see for how it is billed", "the link was taken out of the sentence, not the sentence out of the page");

        // Not asserted here, and it cannot be: no product name appears because the PROMPTS forbid one, and
        // nothing can strip an arbitrary name. A fixture that leaked one would be a fixture failing, not the
        // application, so the obligation is e05s02's and is checked there.
        streamed.Should().NotContain("Desk Shell");
    }

    [Fact] // AIWB-29
    public async Task A_failed_run_keeps_its_text_and_offers_another_go()
    {
        // The scenario that stops mid-answer: the lines are readable and the result never closes. A run that
        // failed before saying anything would leave no text to keep, so this is the only way to reach the
        // state the scenario is about.
        await BeginAsync("truncated");
        await AskAsync("a desk and a chair");

        await Expect(Page.Locator("[data-testid='suggestion-failed']")).ToBeVisibleAsync();

        await Expect(Page.Locator("[data-testid='suggestion-stream']")).ToContainTextAsync("a wide, stable surface");
        await Expect(Page.Locator("[data-testid='suggestion-not-applied']")).ToBeVisibleAsync();
        await Expect(Page.Locator("[data-testid='suggestion-retry']")).ToBeVisibleAsync();
    }

    [Fact] // AIWB-30
    public async Task The_stage_list_is_kept_after_a_run_and_collapsed()
    {
        await BeginAsync("suggested");
        await AskAsync("a desk and a chair");

        await Expect(Page.Locator("[data-testid='suggestion-candidates']")).ToBeVisibleAsync();

        // Retained, so a customer or a support engineer can see where a run went - and collapsed, so it is not
        // what they have to look at once it is over.
        await Expect(Page.Locator("[data-testid='suggestion-stages']")).ToHaveCountAsync(0);
        await Expect(Page.Locator("[data-testid='suggestion-stages-toggle']")).ToBeVisibleAsync();

        await Page.Locator("[data-testid='suggestion-stages-toggle']").ClickAsync();

        await Expect(Page.Locator("[data-testid='suggestion-stages']")).ToBeVisibleAsync();
        await Expect(Page.Locator("[data-testid='suggestion-stages'] li")).ToHaveCountAsync(3);
        await Expect(Page.Locator("[data-testid='suggestion-stages']")).ToContainTextAsync("Reading your request");
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
