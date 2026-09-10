using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Xunit;
using Xunit.Abstractions;

namespace CoreRentalNet.E2E.Flows;

/// <summary>
/// Two browsers, two workspaces. This is the property that a shared draft token silently broke
/// once, and the only way to see it is with two real browser profiles.
/// </summary>
public sealed class WorkspaceIsolationTests(HostFixture host, ITestOutputHelper output) : E2ETest(host, output)
{
    [Fact] // SEC-05
    public async Task One_browser_cannot_see_another_browsers_workspace()
    {
        var otherPage = await Host.NewPageAsync();

        try
        {
            await otherPage.GotoAsync($"{BaseUrl}/builder");
            await Expect(otherPage.Locator("[data-interactive='true']").First).ToBeAttachedAsync();
            await Expect(otherPage.Locator(".workspace-stage")).ToBeVisibleAsync();
            await otherPage.Locator("button.product-card").First.ClickAsync();
            await Expect(otherPage.Locator(".slot--filled")).ToHaveCountAsync(1);

            await GotoAsync("/builder");

            await Expect(Page.Locator(".slot--filled")).ToHaveCountAsync(0);
            await Expect(Page.Locator(".total-bar")).ToContainTextAsync("Rp0");
        }
        finally
        {
            await otherPage.Context.DisposeAsync();
        }
    }

    [Fact] // CO-10
    public async Task Emptying_one_workspace_leaves_the_other_alone()
    {
        var otherPage = await Host.NewPageAsync();

        try
        {
            await otherPage.GotoAsync($"{BaseUrl}/builder");
            await Expect(otherPage.Locator(".workspace-stage")).ToBeVisibleAsync();
            await otherPage.Locator("button.product-card").First.ClickAsync();
            await Expect(otherPage.Locator(".slot--filled")).ToHaveCountAsync(1);

            await AssignFirstProductAsync();
            await GotoAsync("/review");
            await FillAddressAsync();
            await OpenDemoDialogAsync();
            await Page.Locator("#demo-confirmation").FillAsync("this is a demo");
            await DialogConfirmButton.ClickAsync();
            await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/orders/"));

            await GotoAsync("/builder");
            await Expect(Page.Locator(".slot--filled")).ToHaveCountAsync(0);

            // The other browser's cart is untouched by someone else's checkout.
            await Expect(otherPage.Locator(".slot--filled")).ToHaveCountAsync(1);
        }
        finally
        {
            await otherPage.Context.DisposeAsync();
        }
    }
}
