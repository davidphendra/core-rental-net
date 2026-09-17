using System.Net.Http.Json;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Xunit;
using Xunit.Abstractions;

namespace CoreRentalNet.E2E.Flows;

/// <summary>
/// The AI section: asking for a suggestion, watching the run, and stopping it.
/// </summary>
/// <remarks>
/// <para>
/// Against the stand-in, which the fixture starts and points both hosts at, so a run takes the same
/// route a deployed one would: a socket, the protocol's events, and the client the application builds.
/// Nothing here reaches a model, a subscription or the network.
/// </para>
/// <para>
/// The account holds both of the AI permissions, because these tests are about the run and not about
/// who may start one: an account holding only the read permission sees the section and one candidate,
/// which is a rule with its own story.
/// </para>
/// </remarks>
public sealed class AiBuilderStreamTests(HostFixture host, ITestOutputHelper output) : AuthenticatedE2ETest(host, output)
{
    private const string SignedInAccount = "Sari Builder";

    /// <summary>AIB-11 — the run shows its stages while it happens, then the candidates.</summary>
    /// <remarks>
    /// The slow scenario, because the point is the stages being <em>visible during</em> the run. Against
    /// an agent that answers at once the list would already be collapsed by the time the assertion ran,
    /// and the test would pass without ever having seen a stage.
    /// </remarks>
    [Fact]
    public async Task AIB_11_a_submitted_request_shows_stages_then_candidates()
    {
        await SignedInAsync();
        await ScenarioAsync("slow");

        // The announcing region is on the page before the run is, which is the only way it can be
        // observed changing: an element that arrives already full is not announced at all.
        var announcement = Page.Locator("[data-testid='ai-announcement']");

        await Expect(announcement).ToBeAttachedAsync();
        await Expect(announcement).ToBeEmptyAsync();

        await AskAsync("a desk, a chair and a monitor");

        var stages = Page.Locator("[data-testid='ai-stages']");

        await Expect(stages).ToBeVisibleAsync();

        // Announced politely, and never assertively: four stages announced assertively would interrupt
        // a screen reader four times to say nothing a customer has to act on.
        await Expect(announcement).ToHaveAttributeAsync("aria-live", "polite");
        await Expect(announcement).ToContainTextAsync("Checking your request");

        // In the order the run passes through them, in the application's words. A window wide enough
        // that the fourth stage is read while it is on the page rather than after the list collapsed.
        await Expect(stages.Locator(".ai__stage")).ToHaveCountAsync(4, new LocatorAssertionsToHaveCountOptions
        {
            Timeout = 20_000,
        });

        var words = await stages.InnerTextAsync();

        Assert.True(
            words.IndexOf("Checking your request", StringComparison.Ordinal)
                < words.IndexOf("Understanding what you need", StringComparison.Ordinal),
            $"the stages arrived out of order: {words}");

        // Then the candidates, from the same run.
        await Expect(Page.Locator("[data-testid='ai-option']")).ToHaveCountAsync(3, new LocatorAssertionsToHaveCountOptions
        {
            Timeout = 20_000,
        });
    }

    /// <summary>AIB-12 — the list outlives the run, collapsed, and can be opened again.</summary>
    [Fact]
    public async Task AIB_12_the_stage_list_is_retained_after_the_run_and_collapsed()
    {
        await SignedInAsync();
        await ScenarioAsync("essential");

        await AskAsync("a desk, a chair and a monitor");

        await Expect(Page.Locator("[data-testid='ai-option']")).ToHaveCountAsync(3);

        // The options have the focus, so the list is out of the way and still there.
        await Expect(Page.Locator("[data-testid='ai-stages']")).ToHaveCountAsync(0);

        var toggle = Page.Locator("[data-testid='ai-stages-toggle']");

        await Expect(toggle).ToBeVisibleAsync();
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "false");

        await toggle.ClickAsync();

        // The answer to "why did that take so long" survives the run.
        await Expect(Page.Locator("[data-testid='ai-stages'] .ai__stage")).ToHaveCountAsync(4);
    }

    /// <summary>AIB-13 — a cancelled run applies nothing and leaves the workspace alone.</summary>
    /// <remarks>
    /// Nothing is applied until a candidate is chosen, so this is safe by construction rather than by
    /// undoing something - and the assertion is that the construction holds: no candidates, and a canvas
    /// as empty as it was.
    /// </remarks>
    [Fact]
    public async Task AIB_13_cancelling_a_run_applies_nothing()
    {
        await SignedInAsync();
        await ScenarioAsync("slow");

        await Expect(Page.Locator(".slot--filled")).ToHaveCountAsync(0);

        await AskAsync("a desk, a chair and a monitor");

        var cancel = Page.Locator("[data-testid='ai-cancel']");

        await Expect(cancel).ToBeVisibleAsync();
        await cancel.ClickAsync();

        // The control went with the run, so the run ended.
        await Expect(cancel).ToHaveCountAsync(0);

        // Nothing was applied, and no answer was claimed: a cancelled run is not an unavailable agent.
        await Expect(Page.Locator("[data-testid='ai-option']")).ToHaveCountAsync(0);
        await Expect(Page.Locator(".slot--filled")).ToHaveCountAsync(0);
        await Expect(Page.Locator("[data-testid='ai-outcome-none']")).ToHaveCountAsync(0);
    }

    /// <summary>The section is absent, not empty, for an account the entitlement does not cover.</summary>
    /// <remarks>
    /// The section is gated by its own permission rather than the page's, and a closed feature that
    /// renders its own heading and an empty state is an invitation to a customer who cannot accept it.
    /// </remarks>
    [Fact]
    public async Task A_closed_section_is_absent_rather_than_empty()
    {
        await SignInAsync("Dewi Reader");

        await WaitForCircuitAsync(".workspace-stage");

        await Expect(Page.Locator("[data-testid='ai-section']")).ToHaveCountAsync(0);
        await Expect(Page.Locator("[data-testid='ai-query']")).ToHaveCountAsync(0);
    }

    /// <summary>Signed in as the account that holds both AI permissions, with the circuit attached.</summary>
    private async Task SignedInAsync()
    {
        await SignInAsync(SignedInAccount);

        await WaitForCircuitAsync(".workspace-stage");
    }

    private async Task AskAsync(string query)
    {
        await Page.Locator("[data-testid='ai-query']").FillAsync(query);
        await Page.Locator("[data-testid='ai-submit']").ClickAsync();
    }

    /// <summary>
    /// Chooses what the stand-in will answer, so a run's outcome is decided by the test rather than
    /// guessed from the customer's words.
    /// </summary>
    private async Task ScenarioAsync(string name)
    {
        using var client = new HttpClient();

        var response = await client.PostAsJsonAsync($"{Host.AgentUrl}/scenario", new { name });

        response.EnsureSuccessStatusCode();
    }
}
