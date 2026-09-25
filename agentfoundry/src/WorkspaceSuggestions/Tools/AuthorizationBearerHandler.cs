using System.Net.Http.Headers;

namespace WorkspaceSuggestions.Tools;

/// <summary>Puts the call's own token on every MCP request — the handshake, tools/list and tools/call alike.</summary>
/// <remarks>
/// A handler rather than a header captured once: the token differs per call, and the transport is shared within
/// the call. It reads the port, so it knows nothing about where the token is kept.
/// </remarks>
internal sealed class AuthorizationBearerHandler(IMcpAccessTokenService mcpAccessTokenService) : DelegatingHandler
{
    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage httpRequest,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpRequest);

        httpRequest.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await mcpAccessTokenService.GetAsync(cancellationToken));

        return await base.SendAsync(httpRequest, cancellationToken);
    }
}
