using AwesomeAssertions;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace CoreRentalNet.E2E.Flows;

/// <summary>
/// The security headers the application sends, and the promise they make to the browser: the page
/// reaches nothing but its own origin.
/// </summary>
/// <remarks>
/// The policy is proved here rather than assumed. Its failure mode is not an error page - it is a page
/// that renders perfectly and never becomes interactive, because the circuit's socket was refused - so
/// the second test drives a full load, an interactive circuit and a catalogue request, and fails on any
/// violation the browser reports.
/// </remarks>
public sealed class SecurityHeadersTests(HostFixture host, ITestOutputHelper output) : E2ETest(host, output)
{
    [Fact] // SEC-07
    public async Task The_policy_is_delivered_and_admits_no_inline_code()
    {
        var response = await Page.GotoAsync($"{BaseUrl}/");

        var policy = await PolicyAsync(response!);

        policy.Should().Contain("default-src 'self'", "the fallback admits this origin and nothing else");
        policy.Should().Contain("script-src 'self'");
        policy.Should().Contain("style-src 'self'");
        policy.Should().Contain("font-src 'self'");
        policy.Should().Contain("connect-src 'self'", "the circuit is a same-origin socket");
        policy.Should().Contain("frame-ancestors 'none'");
        policy.Should().Contain("object-src 'none'");
        policy.Should().Contain("img-src 'self' data:");

        // The whole point. An inline allowance would let any injected style or script run.
        policy.Should().NotContain("unsafe-inline");
        policy.Should().NotContain("unsafe-eval");
        policy.Should().NotContain("wasm-unsafe-eval");
    }

    [Fact] // SEC-08
    public async Task Nothing_the_page_does_violates_the_policy()
    {
        var violations = new List<string>();

        Page.Console += (_, message) =>
        {
            if (message.Text.Contains("Refused to", StringComparison.Ordinal)
                && message.Text.Contains("Content Security Policy", StringComparison.Ordinal))
            {
                violations.Add(message.Text);
            }
        };

        // The three things that fetch: the page, the circuit's socket, and the catalogue's answers.
        await GotoAsync("/extras", waitFor: ".grid-store");
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Chairs" }).ClickAsync();
        await GotoAsync("/builder", waitFor: ".workspace-stage");

        violations.Should().BeEmpty();
    }

    [Fact] // SEC-09
    public async Task The_companion_headers_are_delivered()
    {
        var response = await Page.GotoAsync($"{BaseUrl}/");

        (await HeaderAsync(response!, "X-Content-Type-Options")).Should().Be("nosniff");
        (await HeaderAsync(response!, "Referrer-Policy")).Should().Be("no-referrer");
        (await HeaderAsync(response!, "Permissions-Policy")).Should().Contain("camera=()");
    }

    private static async Task<string> HeaderAsync(IResponse response, string name)
    {
        var headers = await response.AllHeadersAsync();

        return headers
            .Where(header => header.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
            .Select(header => header.Value)
            .FirstOrDefault() ?? string.Empty;
    }

    private static async Task<string> PolicyAsync(IResponse response)
    {
        var headers = await response.AllHeadersAsync();

        // More than one policy may be delivered - the framework states its own frame-ancestors - and
        // the browser enforces them together, so the test reads them together.
        return headers
            .Where(header => header.Key.StartsWith("content-security-policy", StringComparison.OrdinalIgnoreCase))
            .Select(header => header.Value)
            .Aggregate(string.Empty, (left, right) => left.Length == 0 ? right : $"{left}; {right}");
    }
}
