using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace CoreRentalNet.Agents.Shared.Mcp;

/// <summary>Attaches the call's own catalogue token, and only to a request the policy approved.</summary>
/// <remarks>
/// <b>It refuses rather than sending unauthenticated.</b> A request to an origin the policy did not approve means
/// the transport is wired somewhere the token was never meant to go, and letting it leave without the header would
/// turn that into a confusing refusal at the other end while hiding the mis-wiring.
/// </remarks>
internal sealed class McpAccessTokenHandler(
    IMcpAccessTokenService mcpAccessTokenService,
    CatalogueServerRequestPolicy catalogueServerRequestPolicy,
    ILogger<McpAccessTokenHandler> logger) : DelegatingHandler
{
    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage httpRequest,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpRequest);

        if (httpRequest.RequestUri is not { } requestUri
            || !catalogueServerRequestPolicy.MayCarryTheCallersCatalogueAccessToken(requestUri))
        {
            throw new InvalidOperationException(
                $"An MCP request to '{httpRequest.RequestUri}' is not one the caller's token may be " +
                "attached to.");
        }

        httpRequest.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await mcpAccessTokenService.GetAsync(cancellationToken));

        // The origin and the method are safe to record and the token is not, so only those two are named.
        logger.LogDebug(
            "Attached the caller's token to {Method} {Origin}.",
            httpRequest.Method,
            requestUri.GetLeftPart(UriPartial.Authority));

        return await base.SendAsync(httpRequest, cancellationToken);
    }
}
