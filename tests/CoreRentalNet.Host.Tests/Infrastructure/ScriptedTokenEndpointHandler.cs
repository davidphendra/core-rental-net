using System.Net;
using System.Text;

namespace CoreRentalNet.Host.Tests.Infrastructure;

/// <summary>The token endpoint at the far end of the identity SDK's refresh: it answers what the test says.</summary>
/// <remarks>
/// Written by hand rather than substituted, because what the service tests prove is that the SDK's own refresh
/// is what produced the token - so a real request has to leave the SDK and reach something. No provider is
/// involved: the SDK posts to <c>https://{domain}/oauth/token</c>, and this is what answers it.
/// </remarks>
internal sealed class ScriptedTokenEndpointHandler(HttpStatusCode statusCode, string body) : HttpMessageHandler
{
    /// <summary>An endpoint that hands out the named access token.</summary>
    public static ScriptedTokenEndpointHandler Issuing(string accessToken)
        => new(HttpStatusCode.OK, $$"""{"access_token":"{{accessToken}}","expires_in":3600,"token_type":"Bearer"}""");

    /// <summary>An endpoint that refuses the refresh token it was presented.</summary>
    public static ScriptedTokenEndpointHandler RefusingTheRefreshToken()
        => new(HttpStatusCode.BadRequest, """{"error":"invalid_grant","error_description":"Unknown refresh token."}""");

    /// <summary>Whether the SDK reached this endpoint at all.</summary>
    public bool WasCalled { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        WasCalled = true;

        return Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        });
    }
}
