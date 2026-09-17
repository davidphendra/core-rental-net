using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Xunit;
using Xunit.Abstractions;

namespace CoreRentalNet.E2E.Flows;

/// <summary>
/// Every way a run can end, and who gets to see how many candidates.
/// </summary>
/// <remarks>
/// <para>
/// A run ends in one of four ways, not two, and each is asserted against the stand-in: a refusal, an
/// exhaustion, the application's own failure to reach an agent, and a run that worked. The first of
/// those is why this exists - a refusal and a failure are easy to render as the same thing, and the
/// customer is the one who pays for the difference: only one of them offers another go, because only
/// one of them could have a different answer the second time.
/// </para>
/// <para>
/// The count is asserted with the two accounts the entitlement distinguishes. Every other test in this
/// file runs as the account holding the power permission and therefore sees three candidates, which is
/// what makes the one-candidate case worth its own test rather than its own assumption.
/// </para>
/// </remarks>
public sealed class AiBuilderOutcomeTests(HostFixture host, ITestOutputHelper output) : AiBuilderTest(host, output)
{
    /// <summary>AIB-14 — a refusal is the application's message, no candidates and no retry.</summary>
    /// <remarks>
    /// No retry, and that is the substance of the test: the same words sent again produce the same
    /// refusal, so offering another go would be inviting a customer to repeat themselves for the same
    /// answer. What helps is being told what would work.
    /// </remarks>
    [Fact]
    public async Task AIB_14_a_refusal_shows_the_applications_message_and_no_candidates()
    {
        await SignedInAsync(PowerAccount);
        await ScenarioAsync("rejected");

        await AskAsync("what is the weather like");

        var refusal = Page.Locator("[data-testid='ai-refusal']");

        await Expect(refusal).ToBeVisibleAsync();
        await Expect(refusal).Not.ToContainTextAsync("not_workspace_request");

        await Expect(Options).ToHaveCountAsync(0);
        await Expect(Page.Locator("[data-testid='ai-retry']")).ToHaveCountAsync(0);

        // The stage list is still there, collapsed, so the customer can see where the run stopped.
        await Expect(Page.Locator("[data-testid='ai-stages-toggle']")).ToBeVisibleAsync();
    }

    /// <summary>AIB-15 — an exhaustion is a result with a caveat, and its candidates can be chosen.</summary>
    [Fact]
    public async Task AIB_15_exhaustion_shows_its_candidates_with_the_caveat()
    {
        await SignedInAsync(PowerAccount);
        await ScenarioAsync("exhausted");

        await AskAsync(ADeskAChairAMonitor);

        await Expect(Page.Locator("[data-testid='ai-caveat']")).ToBeVisibleAsync();

        // The findings, said in the application's words and placed on the slot they are about.
        var findings = Page.Locator("[data-testid='ai-findings']");

        await Expect(findings).ToBeVisibleAsync();
        await Expect(findings).ToContainTextAsync("chair");
        await Expect(findings).Not.ToContainTextAsync("criteria_not_met");

        // Exhausted is a result, not a failure: the candidates are here.
        await Expect(Options).ToHaveCountAsync(3);
    }

    /// <summary>AIB-16 — the entitlement decides the count, and the one shown is the middle.</summary>
    /// <remarks>
    /// An account holding the read permission without the power permission. Which candidate is shown is
    /// as much the rule as how many: the neutral choice, neither the cheapest available nor the most
    /// expensive - which is the candidate the contract calls <c>middle</c>.
    /// </remarks>
    [Fact]
    public async Task AIB_16_a_non_power_user_is_shown_one_candidate_and_it_is_the_middle_one()
    {
        await SignedInAsync(OrdinaryAccount);
        await ScenarioAsync("essential");

        await AskAsync(ADeskAChairAMonitor);

        var option = Page.Locator("[data-testid='ai-option']");

        await Expect(option).ToHaveCountAsync(1);
        await Expect(option).ToHaveAttributeAsync("data-tier", "middle");

        // Named as a customer reads it, rather than as the contract spells it.
        await Expect(option).ToContainTextAsync("Balanced");
        await Expect(option).Not.ToContainTextAsync("middle");
    }

    /// <summary>AIB-17 — a criterion the catalogue cannot express comes back as the customer's word.</summary>
    /// <remarks>
    /// The phrase is the only free text in the contract and it comes from the request rather than from
    /// the model, so it is quoted as the customer wrote it. The criteria the option did satisfy are read
    /// out as words: a token on the page would be the agent's vocabulary leaking into the interface.
    /// </remarks>
    [Fact]
    public async Task AIB_17_unevaluated_criteria_are_shown_on_the_candidate()
    {
        await SignedInAsync(PowerAccount);
        await ScenarioAsync("unmet");

        await AskAsync("a desk, a chair and a barefoot friendly monitor");

        var unmet = Page.Locator("[data-testid='ai-unevaluated']").First;

        await Expect(unmet).ToBeVisibleAsync();
        await Expect(unmet).ToContainTextAsync("barefoot");

        // What it did satisfy, as words - and never as the tokens the contract carries.
        await Expect(Page.Locator("[data-testid='ai-criteria']").First).ToContainTextAsync("desk");
        await Expect(Page.Locator("[data-testid='ai-criteria']").First).Not.ToContainTextAsync("slot:desk");
    }

    /// <summary>
    /// An agent that cannot be reached says so and offers another go, and the retry works.
    /// </summary>
    /// <remarks>
    /// The only outcome the application reaches on its own, and the only one where trying again could
    /// have a different answer. The test does not stop at the button being there: it fixes the agent and
    /// presses it, because a control that renders and does nothing is worse than no control.
    /// </remarks>
    [Fact]
    public async Task An_unreachable_agent_says_so_and_offers_a_retry_that_works()
    {
        await SignedInAsync(PowerAccount);
        await ScenarioAsync("broken");

        await AskAsync(ADeskAChairAMonitor);

        var error = Page.Locator("[data-testid='ai-error']");

        await Expect(error).ToBeVisibleAsync();
        await Expect(Options).ToHaveCountAsync(0);

        // Nothing was applied, and the message says so rather than leaving the customer to guess.
        await Expect(Page.Locator(".slot--filled")).ToHaveCountAsync(0);

        await ScenarioAsync("essential");
        await Page.Locator("[data-testid='ai-retry']").ClickAsync();

        await Expect(Options).ToHaveCountAsync(3);
        await Expect(error).ToHaveCountAsync(0);
    }
}
