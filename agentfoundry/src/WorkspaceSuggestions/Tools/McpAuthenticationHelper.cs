using ModelContextProtocol.Client;

namespace WorkspaceSuggestions.Tools;

/// <summary>Builds an MCP client that authenticates every request as the caller.</summary>
/// <remarks>
/// A factory, not a shared connection: the MCP handshake is itself authenticated, so the client exists only
/// while a call's token does.
/// </remarks>
internal static class McpAuthenticationHelper
{
    /// <summary>The authenticated client for one catalogue endpoint.</summary>
    public static async Task<McpClient?> ConnectAsync(
        Uri mcpEndpointUri,
        IMcpAccessTokenService mcpAccessTokenService,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mcpEndpointUri);
        ArgumentNullException.ThrowIfNull(mcpAccessTokenService);

        return await McpClient.CreateAsync(
            new HttpClientTransport(
                new HttpClientTransportOptions
                {
                    Endpoint = mcpEndpointUri,

                    // Named by where it is, so the name cannot say "catalogue" for a deployment pointed
                    // somewhere else.
                    Name = mcpEndpointUri.ToString(),
                },
                new HttpClient(new AuthorizationBearerHandler(mcpAccessTokenService)),
                null,
                false),
            cancellationToken: cancellationToken);
    }
}
