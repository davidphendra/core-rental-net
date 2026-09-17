using System.Net.Http.Json;
using Microsoft.Playwright;
using Xunit.Abstractions;

namespace CoreRentalNet.E2E.Flows;

/// <summary>
/// The steps every test about the AI section takes.
/// </summary>
/// <remarks>
/// The two accounts are the two answers the entitlement can give: one holds the power permission and is
/// shown all three candidates, and one holds only the read permission and is shown the middle one.
/// </remarks>
public abstract class AiBuilderTest(HostFixture host, ITestOutputHelper output) : AuthenticatedE2ETest(host, output)
{
    /// <summary>Holds both AI permissions, so a run shows all three candidates.</summary>
    protected const string PowerAccount = "Sari Builder";

    /// <summary>Holds the read permission only, so a run shows the middle candidate.</summary>
    protected const string OrdinaryAccount = "Andi Viewer";

    protected const string ADeskAChairAMonitor = "a desk, a chair and a monitor";

    protected ILocator Options => Page.Locator("[data-testid='ai-option']");

    /// <summary>Signed in as an account, with the circuit attached.</summary>
    protected async Task SignedInAsync(string account)
    {
        await SignInAsync(account);

        await WaitForCircuitAsync(".workspace-stage");
    }

    protected async Task AskAsync(string query)
    {
        await Page.Locator("[data-testid='ai-query']").FillAsync(query);
        await Page.Locator("[data-testid='ai-submit']").ClickAsync();
    }

    /// <summary>
    /// Chooses what the stand-in will answer, so a run's outcome is decided by the test rather than
    /// guessed from the customer's words.
    /// </summary>
    protected async Task ScenarioAsync(string name)
    {
        using var client = new HttpClient();

        var response = await client.PostAsJsonAsync($"{Host.AgentUrl}/scenario", new { name });

        response.EnsureSuccessStatusCode();
    }
}
