using System.Text.Json;
using AwesomeAssertions;
using CoreRentalNet.Host.Presentation;
using Microsoft.Extensions.Configuration;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// What the documentation tests share: the identity the page is configured from, and a reader for the
/// configuration swashbuckle injects into its own script.
/// </summary>
internal static class SwaggerTestSetup
{
    public const string ClientId = "swagger-client";

    public const string Audience = "https://catalogue.example";

    public const string Scope = "openid profile email read:catalog";

    /// <summary>The identity configuration a test host is built from, with known values.</summary>
    public static Dictionary<string, string?> Settings(
        string clientId = ClientId,
        string audience = Audience,
        string scope = Scope) => new()
    {
        ["Auth0:Enabled"] = "true",
        ["Auth0:Domain"] = "tenant.example",
        ["Auth0:ClientId"] = clientId,
        ["Auth0:Audience"] = audience,
        ["Auth0:Scope"] = scope,
    };

    /// <summary>The same configuration as the settings type the application reads.</summary>
    public static IdentitySettings Identity(
        string clientId = ClientId,
        string audience = Audience,
        string scope = Scope)
        => IdentitySettings.From(new ConfigurationBuilder()
            .AddInMemoryCollection(Settings(clientId, audience, scope))
            .Build());

    /// <summary>
    /// One of the two configuration objects swashbuckle injects into its script, which it writes as a
    /// JSON string inside a call: <c>var name = JSON.parse('{…}');</c>.
    /// </summary>
    /// <remarks>
    /// The marker is asserted before slicing: without that, a change to how the page is generated
    /// would fail here as an out-of-range substring rather than as a statement about the page.
    /// </remarks>
    public static async Task<JsonElement> InjectedConfigAsync(HttpClient client, string name)
    {
        ArgumentNullException.ThrowIfNull(client);

        var script = await client.GetStringAsync("/swagger/index.js");
        var marker = $"var {name} = JSON.parse('";
        var start = script.IndexOf(marker, StringComparison.Ordinal);

        start.Should().BeGreaterThanOrEqualTo(
            0,
            $"swashbuckle injects {name} into its script, and without it these assertions prove nothing");

        var end = script.IndexOf("')", start, StringComparison.Ordinal);

        end.Should().BeGreaterThan(start, $"the injected {name} is a JSON string that ends");

        return JsonDocument.Parse(script[(start + marker.Length)..end]).RootElement;
    }
}
