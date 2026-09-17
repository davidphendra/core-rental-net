using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Xunit;
using Xunit.Abstractions;

namespace CoreRentalNet.E2E.Flows;

/// <summary>
/// The run guard, driven through the page rather than through the service.
/// </summary>
/// <remarks>
/// The unit tests hold the rule. This holds the thing the rule exists for, which is that a customer who
/// stops a run can start another one - if the guard outlived a cancel, the feature would punish anybody
/// who used the control this epic added for them. The suite runs with no cooldown, so what these tests
/// see is the one-in-flight rule and nothing else.
/// </remarks>
public sealed class AiBuilderGuardTests(HostFixture host, ITestOutputHelper output) : AiBuilderTest(host, output)
{
    /// <summary>AIB-23 — the guard is released when the run ends, including when it is cancelled.</summary>
    [Fact]
    public async Task AIB_23_the_guard_is_released_when_the_run_ends_including_on_cancel()
    {
        await SignedInAsync(PowerAccount);
        await ScenarioAsync("slow");

        // The first run, stopped rather than finished.
        await AskAsync(ADeskAChairAMonitor);

        var cancel = Page.Locator("[data-testid='ai-cancel']");

        await Expect(cancel).ToBeVisibleAsync();
        await cancel.ClickAsync();
        await Expect(cancel).ToHaveCountAsync(0);

        // And immediately again, with no wait: the point is that a run that was stopped released what it
        // held. A guard that only released on completion would refuse this one, and a customer who used
        // the cancel control would be locked out of the feature for it.
        await AskAsync(ADeskAChairAMonitor);

        await Expect(cancel).ToBeVisibleAsync();
        await Expect(Page.Locator("[data-testid='ai-refused']")).ToHaveCountAsync(0);
        await Expect(Page.Locator("[data-testid='ai-stages']")).ToBeVisibleAsync();

        await cancel.ClickAsync();
    }

    /// <summary>
    /// A second submit while one is in flight is refused, in the application's own words.
    /// </summary>
    /// <remarks>
    /// The realistic path to twenty model calls is a double click, so this drives one: the second submit
    /// is refused by the page and nothing reaches the agent for it.
    /// </remarks>
    [Fact]
    public async Task A_double_submit_is_refused_by_the_page()
    {
        await SignedInAsync(PowerAccount);
        await ScenarioAsync("slow");

        await Page.Locator("[data-testid='ai-query']").FillAsync(ADeskAChairAMonitor);

        var submit = Page.Locator("[data-testid='ai-submit']");

        await submit.ClickAsync();
        await Expect(Page.Locator("[data-testid='ai-cancel']")).ToBeVisibleAsync();

        // The Enter key a customer presses because the page looks slow. The submit control is disabled
        // while a run is happening, so this asks through the same path the click would have taken.
        var refused = Page.Locator("[data-testid='ai-refused']");

        await Page.EvaluateAsync(
            "() => document.querySelector(\"[data-testid='ai-submit']\").removeAttribute('disabled')");

        await submit.ClickAsync();

        await Expect(refused).ToHaveCountAsync(1);
        await Expect(refused).ToContainTextAsync("still being worked on");
    }
}
