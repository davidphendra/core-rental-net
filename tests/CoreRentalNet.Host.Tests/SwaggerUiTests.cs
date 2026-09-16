using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The swagger-ui page: served in development, configured from the identity settings the application
/// already holds, and served under a policy that admits the page it is.
/// </summary>
public sealed class SwaggerUiTests(CatalogOpenApiFactory factory) : IClassFixture<CatalogOpenApiFactory>
{
    [Fact] // API-15
    public async Task The_page_is_served_in_development()
    {
        var response = await factory.CreateClient().GetAsync("/swagger/index.html");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("swagger-ui");
    }

    [Fact] // API-15
    public async Task The_page_is_configured_from_the_identity_settings()
    {
        // The two configuration objects are injected into the page's own script, not into its markup.
        var script = await factory.CreateClient().GetStringAsync("/swagger/index.js");
        var oauth = Config(script, "oauthConfigObject");

        // The client the application signs people in with, and the scopes the login asks for - a
        // permission the provider was not asked for is one it will not issue.
        oauth.GetProperty("clientId").GetString().Should().Be("swagger-client");
        oauth.GetProperty("scopes").EnumerateArray().Select(scope => scope.GetString())
            .Should().Contain("read:catalog");

        // The audience the permission is issued against. Without it the provider issues a token
        // carrying no permissions and the endpoint answers 403.
        oauth.GetProperty("additionalQueryStringParams").GetProperty("audience").GetString()
            .Should().Be("https://catalogue.example");

        // PKCE is what lets a browser do this without a client secret reaching the page.
        oauth.GetProperty("usePkceWithAuthorizationCodeGrant").GetBoolean().Should().BeTrue();

        // The redirect address is left to swagger-ui, which derives it from the page's own address -
        // right on whatever port the application was launched on, and nothing to keep in step.
        Config(script, "configObject").TryGetProperty("oauth2RedirectUrl", out _).Should().BeFalse();
    }

    /// <summary>
    /// One of the two configuration objects swashbuckle injects into its script, which it writes as a
    /// JSON string inside a call: <c>var name = JSON.parse('{…}');</c>.
    /// </summary>
    private static JsonElement Config(string script, string name)
    {
        var marker = $"var {name} = JSON.parse('";
        var start = script.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var end = script.IndexOf("')", start, StringComparison.Ordinal);

        return JsonDocument.Parse(script[start..end]).RootElement;
    }

    [Fact] // API-16
    public async Task The_documentation_is_served_under_its_own_policy_and_nothing_else_is()
    {
        var relaxed = await PolicyAsync("/swagger/index.html");
        var strict = await PolicyAsync("/");

        // swagger-ui builds itself from an inline script and inline styles, which the strict policy
        // refuses; without this exception the page renders blank, which looks like a working page.
        relaxed.Should().Contain("'unsafe-inline'");
        relaxed.Should().Contain("frame-ancestors 'none'", "the exception is about inline code, not framing");

        // And the exception reaches nothing else. Every application page keeps the strict policy.
        strict.Should().NotContain("'unsafe-inline'");
    }

    private async Task<string> PolicyAsync(string path)
    {
        var response = await factory.CreateClient().GetAsync(path);

        response.EnsureSuccessStatusCode();

        return response.Headers.TryGetValues("Content-Security-Policy", out var values)
            ? string.Join("; ", values)
            : string.Empty;
    }
}
