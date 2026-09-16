using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The swagger-ui page: served in development, pointed at the document this application actually
/// serves, configured from the identity settings it already holds, and served under a policy that
/// admits the page it is.
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

    [Fact] // API-17
    public async Task The_page_is_pointed_at_the_document_the_application_serves()
    {
        var client = factory.CreateClient();

        var urls = (await SwaggerTestSetup.InjectedConfigAsync(client, "configObject"))
            .GetProperty("urls")
            .EnumerateArray()
            .Select(entry => entry.GetProperty("url").GetString())
            .ToArray();

        urls.Should().ContainSingle();

        // The page loads whatever this names, so the test loads it too. Swashbuckle's default is
        // v1/swagger.json, which this application does not serve - and a page whose document is a 404
        // renders, shows nothing, and looks fine. That is exactly what it did before this existed.
        var document = await client.GetAsync(new Uri(new Uri(client.BaseAddress!, "/swagger/index.html"), urls[0]!));

        document.StatusCode.Should().Be(HttpStatusCode.OK);
        (await document.Content.ReadAsStringAsync()).Should().Contain("/api/catalog");
    }

    [Fact] // API-15
    public async Task The_page_is_configured_from_the_identity_settings()
    {
        var oauth = await SwaggerTestSetup.InjectedConfigAsync(factory.CreateClient(), "oauthConfigObject");

        // The client the application signs people in with, and the scopes the login asks for - a
        // permission the provider was not asked for is one it will not issue.
        oauth.GetProperty("clientId").GetString().Should().Be(SwaggerTestSetup.ClientId);
        oauth.GetProperty("scopes").EnumerateArray().Select(scope => scope.GetString())
            .Should().Contain("read:catalog");

        // The audience the permission is issued against. Without it the provider issues a token
        // carrying no permissions and the endpoint answers 403.
        oauth.GetProperty("additionalQueryStringParams").GetProperty("audience").GetString()
            .Should().Be(SwaggerTestSetup.Audience);

        // PKCE is what lets a browser do this without a client secret reaching the page.
        oauth.GetProperty("usePkceWithAuthorizationCodeGrant").GetBoolean().Should().BeTrue();
    }

    [Fact] // API-16
    public async Task The_documentation_policy_differs_from_the_strict_one_only_where_it_must()
    {
        var relaxed = Directives(await PolicyAsync("/swagger/index.html"));
        var strict = Directives(await PolicyAsync("/"));

        strict.Keys.Should().BeEquivalentTo(
            relaxed.Keys,
            "the exception widens directives rather than adding or dropping them");

        var widened = strict.Keys
            .Where(name => !string.Equals(strict[name], relaxed[name], StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        // swagger-ui builds itself from an inline script and inline styles; the authorization-code
        // exchange is a fetch from the browser to the provider's token endpoint, which connect-src
        // governs. Everything else is identical, framing included.
        widened.Should().Equal(["connect-src", "script-src", "style-src"]);
        relaxed["script-src"].Should().Contain("'unsafe-inline'");
        relaxed["style-src"].Should().Contain("'unsafe-inline'");
        relaxed["connect-src"].Should().Contain(
            "https://tenant.example",
            "without the provider's origin the browser refuses the token exchange");
        relaxed["frame-ancestors"].Should().Be("'none'");
    }

    /// <summary>The directives of one policy, by name.</summary>
    private static Dictionary<string, string> Directives(string policy)
        => policy
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries))
            .ToDictionary(parts => parts[0], parts => parts.Length > 1 ? parts[1] : string.Empty);

    private async Task<string> PolicyAsync(string path)
    {
        var response = await factory.CreateClient().GetAsync(path);

        response.EnsureSuccessStatusCode();

        // The application's own policy. A component endpoint may carry a second, framework-written one,
        // which is enforced alongside it rather than instead of it.
        return response.Headers.TryGetValues("Content-Security-Policy", out var values)
            ? values.First()
            : string.Empty;
    }
}
