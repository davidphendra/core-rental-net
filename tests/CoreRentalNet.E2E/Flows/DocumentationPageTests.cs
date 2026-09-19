using AwesomeAssertions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Xunit;
using Xunit.Abstractions;

namespace CoreRentalNet.E2E.Flows;

/// <summary>
/// The development documentation page, driven in a browser.
/// </summary>
/// <remarks>
/// The page is useless if it loads and lists nothing, and that is exactly what it did while it was
/// pointed at a document this application does not serve - a failure no amount of fetching and
/// checking status codes can see. Two things are asserted instead: the operation is on the page, and
/// the browser accepted the policy the page was served under.
/// </remarks>
public sealed class DocumentationPageTests(HostFixture host, ITestOutputHelper output) : E2ETest(host, output)
{
    [Fact] // API-19
    public async Task The_catalogue_endpoint_is_listed_and_the_policy_admits_the_page()
    {
        var refusals = new List<string>();

        Page.Console += (_, message) =>
        {
            if (message.Text.Contains("Refused to", StringComparison.Ordinal)
                && message.Text.Contains("Content Security Policy", StringComparison.Ordinal))
            {
                refusals.Add(message.Text);
            }
        };

        // Not GotoAsync: the documentation is not a Blazor route, so there is no interactive circuit
        // to wait for.
        await Page.GotoAsync($"{BaseUrl}/swagger/index.html");

        await Expect(Page.Locator(".opblock").First)
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });

        // Found by its path rather than taken as the first operation on the page. The document lists
        // every endpoint the application serves, and "the first one is the catalogue" was a fact about
        // the document having one endpoint rather than anything this test is checking - it broke the
        // day a second endpoint was added, which is exactly how a test that is really asserting an
        // ordering fails.
        var catalogue = Page.Locator(".opblock-summary-path[data-path='/api/catalog']");

        await Expect(catalogue)
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });

        await Expect(catalogue).ToContainTextAsync("/api/catalog");

        refusals.Should().BeEmpty("the page is served under a policy that must admit the page itself");
    }
}
