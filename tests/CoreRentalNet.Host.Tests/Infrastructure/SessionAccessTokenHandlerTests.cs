using System.Net.Http.Headers;
using Auth0.AspNetCore.Authentication;
using AwesomeAssertions;
using CoreRentalNet.Host.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CoreRentalNet.Host.Tests.Infrastructure;

/// <summary>The outbound handler presents the token the identity SDK produced, and nothing when it produced none.</summary>
/// <remarks>
/// The handler asks the SDK rather than reading the ticket, so a token that has expired is refreshed before it is
/// attached - and a session that cannot produce one sends no bearer at all rather than a dead one.
/// </remarks>
public sealed class SessionAccessTokenHandlerTests
{
    private const string TheApi = "https://api.example/resource";

    [Fact]
    public async Task The_handler_attaches_the_token_the_identity_sdk_produced()
    {
        var request = await SendAsync(withAccessToken: true);

        request.Headers.Authorization.Should().Be(
            new AuthenticationHeaderValue("Bearer", CatalogApiTestHandler.AccessToken));
    }

    [Fact]
    public async Task The_handler_attaches_nothing_when_the_session_cannot_produce_a_token()
    {
        var request = await SendAsync(withAccessToken: false);

        request.Headers.Authorization.Should().BeNull(
            "an expired token is not a credential, and the API's own 401 is the honest answer");
    }

    private static async Task<HttpRequestMessage> SendAsync(bool withAccessToken)
    {
        await using var provider = ProviderForTheIdentitySdk();
        var httpContext = new DefaultHttpContext { RequestServices = provider };

        httpContext.Request.Headers[CatalogApiTestHandler.PermissionsHeader] = "read:catalog";

        if (!withAccessToken)
        {
            httpContext.Request.Headers[CatalogApiTestHandler.WithoutAccessTokenHeader] = "true";
        }

        var recording = new RecordingOutboundRequestHandler();

        using var handler = new TokenHandler(new HttpContextAccessor { HttpContext = httpContext })
        {
            InnerHandler = recording,
        };

        using var client = new HttpClient(handler);

        using var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, TheApi));

        response.EnsureSuccessStatusCode();

        return recording.Request!;
    }

    /// <summary>The SDK's own services over the test sign-in, with no provider reachable.</summary>
    private static ServiceProvider ProviderForTheIdentitySdk()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddOptions();
        services.AddAuthentication(CatalogApiTestHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, CatalogApiTestHandler>(
                CatalogApiTestHandler.SchemeName,
                _ => { });

        services.Configure<Auth0WebAppOptions>(Auth0Constants.AuthenticationScheme, options =>
        {
            options.Domain = "tenant.example";
            options.ClientId = "client";
            options.CookieAuthenticationScheme = CatalogApiTestHandler.SchemeName;
        });

        services.Configure<Auth0WebAppWithAccessTokenOptions>(Auth0Constants.AuthenticationScheme, options =>
        {
            options.Audience = "https://corerental/api";
            options.UseRefreshTokens = true;
            options.Events = null;
        });

        return services.BuildServiceProvider();
    }
}
