using AwesomeAssertions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Xunit;
using Xunit.Abstractions;

namespace CoreRentalNet.E2E.Flows;

/// <summary>
/// AIWB-36 and AIWB-37: choosing a candidate, and what it does to the workspace and to the address.
/// </summary>
/// <remarks>
/// <para>
/// The write itself is asserted where it lives - the module's own tests cover one atomic replacement, the
/// stale-version refusal (AIWB-38) and the address surviving. What is asserted here is the path a customer
/// takes: the confirmation appears only when there is something to lose, and what it says is true.
/// </para>
/// <para>
/// Nothing here is paced by the fixture, so a run finishes immediately; every assertion is about the state
/// after it, never about catching it mid-flight.
/// </para>
/// </remarks>
public sealed class AiBuilderApplyTests : AuthenticatedE2ETest
{
    private readonly ITestOutputHelper output;

    public AiBuilderApplyTests(HostFixture host, ITestOutputHelper output)
        : base(host, output)
    {
        this.output = output;
    }

    [Fact] // AIWB-36
    public async Task Choosing_a_candidate_over_a_full_workspace_confirms_and_then_replaces_it()
    {
        await BeginAsync();
        await AssignADeskAndAChairAsync();

        await AskAsync("a desk, a chair and a monitor");
        await Expect(Page.Locator("[data-testid='ai-candidates']")).ToBeVisibleAsync();

        await Expect(Page.Locator(".slot--filled")).ToHaveCountAsync(2);

        await Page.Locator("[data-testid='ai-apply']").First.ClickAsync();

        // Asked, because there is something to lose - and it names how much, so the choice is an informed one.
        await Expect(Page.Locator("dialog[open]")).ToBeVisibleAsync();

        var asked = await Page.Locator("[data-testid='apply-confirm-body']").InnerTextAsync();

        output.WriteLine(asked);
        asked.Should().Contain("2 items");
        asked.Should().Contain("delivery address is not changed", "that is the fear a customer has at this moment");

        await Page.Locator("[data-testid='apply-confirm']").ClickAsync();

        // Replaced: the candidate is a desk, a chair and a monitor, so the canvas holds three.
        await Expect(Page.Locator("[data-testid='ai-applied']")).ToBeVisibleAsync();
        await Expect(Page.Locator(".slot--filled")).ToHaveCountAsync(3);
    }

    [Fact] // AIWB-36's other half, from the story: a confirmation that guards nothing is a step to click through
    public async Task Choosing_a_candidate_on_an_empty_workspace_applies_without_asking()
    {
        await BeginAsync();

        await Expect(Page.Locator(".slot--filled")).ToHaveCountAsync(0);

        await AskAsync("a desk, a chair and a monitor");
        await Expect(Page.Locator("[data-testid='ai-candidates']")).ToBeVisibleAsync();

        await Page.Locator("[data-testid='ai-apply']").First.ClickAsync();

        // No dialog at all, and the workspace is set up.
        await Expect(Page.Locator("dialog[open]")).ToHaveCountAsync(0);
        await Expect(Page.Locator("[data-testid='ai-applied']")).ToBeVisibleAsync();
        await Expect(Page.Locator(".slot--filled")).ToHaveCountAsync(3);
    }

    [Fact] // AIWB-37
    public async Task Replacing_the_workspace_leaves_the_delivery_address_alone()
    {
        await BeginAsync();
        await AssignADeskAndAChairAsync();

        // The address is set where the page asks for it - the review step - and then the customer goes back to
        // the builder, which is where a suggestion is drawn and applied.
        await GotoAsync("/review");
        await FillAddressAsync("Villa Lotus, Canggu");

        await GotoAsync("/builder");
        await AskAsync("a desk, a chair and a monitor");
        await Expect(Page.Locator("[data-testid='ai-candidates']")).ToBeVisibleAsync();

        await Page.Locator("[data-testid='ai-apply']").First.ClickAsync();
        await Expect(Page.Locator("dialog[open]")).ToBeVisibleAsync();
        await Page.Locator("[data-testid='apply-confirm']").ClickAsync();

        await Expect(Page.Locator("[data-testid='ai-applied']")).ToBeVisibleAsync();
        await Expect(Page.Locator(".slot--filled")).ToHaveCountAsync(3);

        // The workspace was replaced. The address was not: a customer who asked for a different setup did not
        // ask to be re-addressed, and this is the assertion that says so through the whole path.
        await GotoAsync("/review");
        await Expect(Page.Locator("#delivery-address")).ToHaveValueAsync("Villa Lotus, Canggu");
    }

    [Fact] // the other half of the confirmation: keeping what you have changes nothing
    public async Task Keeping_the_workspace_applies_nothing()
    {
        await BeginAsync();
        await AssignADeskAndAChairAsync();

        await AskAsync("a desk, a chair and a monitor");
        await Expect(Page.Locator("[data-testid='ai-candidates']")).ToBeVisibleAsync();

        await Page.Locator("[data-testid='ai-apply']").First.ClickAsync();
        await Expect(Page.Locator("dialog[open]")).ToBeVisibleAsync();

        await Page.Locator("[data-testid='apply-cancel']").ClickAsync();

        await Expect(Page.Locator("dialog[open]")).ToHaveCountAsync(0);

        // Nothing was applied: the workspace still holds the desk and the chair the customer picked.
        await Expect(Page.Locator(".slot--filled")).ToHaveCountAsync(2);
        await Expect(Page.Locator("[data-testid='ai-applied']")).ToHaveCountAsync(0);
    }

    private async Task BeginAsync()
    {
        await Host.ChooseScenarioAsync("suggested");

        // "Dewi Reader" holds the AI permission.
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
