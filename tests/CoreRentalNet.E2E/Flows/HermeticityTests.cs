using AwesomeAssertions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Xunit;
using Xunit.Abstractions;

namespace CoreRentalNet.E2E.Flows;

/// <summary>
/// The suite runs against the application and nothing else. No interception, and no third party.
/// </summary>
public sealed class HermeticityTests(HostFixture host, ITestOutputHelper output) : E2ETest(host, output)
{
    [Fact] // UI-06
    public async Task A_whole_funnel_reaches_nothing_but_the_application()
    {
        await AssignFirstProductAsync();
        await GotoAsync("/extras");
        await GotoAsync("/review");
        await FillAddressAsync();
        await OpenDemoDialogAsync();
        await Page.Locator("#demo-confirmation").FillAsync("this is a demo");
        await DialogConfirmButton.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/orders/"));

        Requests.Should().NotBeEmpty();
        AssertNoThirdPartyRequests();
    }

    [Fact] // UI-06
    public async Task The_design_system_fonts_are_served_by_the_application()
    {
        await GotoAsync("/");

        var fontRequests = Requests
            .Where(url => url.Contains("fonts/", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        fontRequests.Should().NotBeEmpty("the fonts are self-hosted, so the browser asks this application for them");

        foreach (var font in fontRequests)
        {
            font.Should().Contain(BaseUrl, "a font must not come from someone else's server");
        }
    }

    [Fact] // UI-06
    public async Task Product_images_are_local()
    {
        await GotoAsync("/extras");

        var sources = await Page.Locator("img").EvaluateAllAsync<string[]>(
            "images => images.map(image => image.getAttribute('src') || '')");

        sources.Should().NotBeEmpty("the extras page shows images");
        sources.Should().OnlyContain(source =>
            !source.StartsWith("http", StringComparison.OrdinalIgnoreCase),
            "every product image is vendored or drawn locally");
    }

    [Fact] // UI-06
    public async Task Nothing_sits_between_the_browser_and_the_application()
    {
        await GotoAsync("/builder");

        // No service worker rewriting requests, and every response a real one from the server.
        // This asserts the absence of the machinery that would make the other tests meaningless.
        var workers = await Page.EvaluateAsync<int>(
            "async () => (await navigator.serviceWorker.getRegistrations()).length");

        workers.Should().Be(0, "a worker in the middle would be interception by another name");

        var status = await Page.EvaluateAsync<int>("async () => (await fetch('/builder')).status");
        status.Should().Be(200, "the browser talks to the server directly");
    }
}
