using AwesomeAssertions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Xunit;
using Xunit.Abstractions;

namespace CoreRentalNet.E2E.Flows;

/// <summary>
/// The run as a customer watches it: the model's words arriving, the answer after them, and what is left on
/// screen when a run is stopped or fails.
/// </summary>
/// <remarks>
/// <para>
/// Every test drives the section through the page rather than through the endpoint, and the answer is chosen
/// by the test rather than guessed from the customer's words - so a test about a failure does not have to
/// phrase its way into one.
/// </para>
/// <para>
/// The scenario that streams is the one most of these use, because a run that answered instantly would turn
/// the interesting assertions - text with the run still going, a cancellation with something to interrupt -
/// into races rather than checks.
/// </para>
/// </remarks>
public sealed class AiBuilderStreamTests : AuthenticatedE2ETest
{
    private readonly ITestOutputHelper output;

    public AiBuilderStreamTests(HostFixture host, ITestOutputHelper output)
        : base(host, output)
    {
        this.output = output;
    }

    [Fact] // AIWB-25
    public async Task The_model_s_words_arrive_before_the_candidates_do()
    {
        await BeginAsync("slow");

        await AskAsync("a desk and a chair, for a small room");

        // The words are on screen WHILE the run is going, which is the whole point of streaming them: a
        // blank 45-second wait is what this replaces. The Stop control is the proof the run has not ended.
        await Expect(Page.Locator("[data-testid='ai-stream']")).ToContainTextAsync("uncluttered setup");
        await Expect(Page.Locator("[data-testid='ai-cancel']")).ToBeVisibleAsync();

        // And the answer arrives after them, once and whole.
        await Expect(Page.Locator("[data-testid='ai-candidates']")).ToBeVisibleAsync();
        await Expect(Page.Locator("[data-testid='ai-option']")).ToHaveCountAsync(3);
    }

    [Fact] // AIWB-26
    public async Task Streamed_text_carries_no_price_and_no_product_name()
    {
        await BeginAsync("leaky");
        await AskAsync("a desk and a chair");

        await Expect(Page.Locator("[data-testid='ai-candidates']")).ToBeVisibleAsync();

        var streamed = await Page.Locator("[data-testid='ai-stream']").InnerTextAsync();

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

    [Fact] // AIWB-27
    public async Task Stopping_is_reported_as_stopped_and_applies_nothing()
    {
        await BeginAsync("slow");
        await AskAsync("a desk and a chair");

        await Expect(Page.Locator("[data-testid='ai-stream']")).ToContainTextAsync("uncluttered setup");

        await Page.Locator("[data-testid='ai-cancel']").ClickAsync();

        await Expect(Page.Locator("[data-testid='ai-stopped']")).ToBeVisibleAsync();

        // Neutral, not an error: the customer used the control that is there to be used.
        await Expect(Page.Locator("[data-testid='ai-failed']")).ToHaveCountAsync(0);

        // And nothing was applied, because nothing could have been.
        await Expect(Page.Locator("[data-testid='ai-candidates']")).ToHaveCountAsync(0);
    }

    [Fact] // AIWB-28
    public async Task Stopping_leaves_the_streamed_text_in_place_marked_not_applied()
    {
        await BeginAsync("slow");
        await AskAsync("a desk and a chair");

        await Expect(Page.Locator("[data-testid='ai-stream']")).ToContainTextAsync("uncluttered setup");

        await Page.Locator("[data-testid='ai-cancel']").ClickAsync();
        await Expect(Page.Locator("[data-testid='ai-stopped']")).ToBeVisibleAsync();

        // Kept rather than retracted mid-read, and marked, because what a customer who stopped needs to know
        // is whether their workspace moved. It did not.
        await Expect(Page.Locator("[data-testid='ai-stream']")).ToContainTextAsync("uncluttered setup");
        await Expect(Page.Locator("[data-testid='ai-not-applied']")).ToBeVisibleAsync();
    }

    [Fact] // AIWB-29
    public async Task A_failed_run_keeps_its_text_and_offers_another_go()
    {
        // The scenario that stops mid-answer: the lines are readable and the result never closes. A run that
        // failed before saying anything would leave no text to keep, so this is the only way to reach the
        // state the scenario is about.
        await BeginAsync("truncated");
        await AskAsync("a desk and a chair");

        await Expect(Page.Locator("[data-testid='ai-failed']")).ToBeVisibleAsync();

        await Expect(Page.Locator("[data-testid='ai-stream']")).ToContainTextAsync("a wide, stable surface");
        await Expect(Page.Locator("[data-testid='ai-not-applied']")).ToBeVisibleAsync();
        await Expect(Page.Locator("[data-testid='ai-retry']")).ToBeVisibleAsync();
    }

    [Fact] // AIWB-30
    public async Task The_stage_list_is_kept_after_a_run_and_collapsed()
    {
        await BeginAsync("suggested");
        await AskAsync("a desk and a chair");

        await Expect(Page.Locator("[data-testid='ai-candidates']")).ToBeVisibleAsync();

        // Retained, so a customer or a support engineer can see where a slow or partly failed run went -
        // and collapsed, so it is not what they have to look at once the run is over.
        await Expect(Page.Locator("[data-testid='ai-stages']")).ToHaveCountAsync(0);
        await Expect(Page.Locator("[data-testid='ai-stages-toggle']")).ToBeVisibleAsync();

        await Page.Locator("[data-testid='ai-stages-toggle']").ClickAsync();

        await Expect(Page.Locator("[data-testid='ai-stages']")).ToBeVisibleAsync();
        await Expect(Page.Locator("[data-testid='ai-stages'] li")).ToHaveCountAsync(3);
        await Expect(Page.Locator("[data-testid='ai-stages']")).ToContainTextAsync("Reading your request");
    }

    private async Task BeginAsync(string scenario)
    {
        await Host.ChooseScenarioAsync(scenario);

        // "Dewi Reader" is the account that holds the AI permission. One without it never sees the section
        // at all, which is what a closed feature looks like from the outside.
        await SignInAsync("Dewi Reader");
        await WaitForCircuitAsync(".workspace-stage");

        await Expect(Page.Locator("[data-testid='ai-section']")).ToBeVisibleAsync();
    }

    private async Task AskAsync(string query)
    {
        await Page.Locator("[data-testid='ai-query']").FillAsync(query);
        await Page.Locator("[data-testid='ai-submit']").ClickAsync();
    }
}
