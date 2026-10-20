using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;

namespace CoreRentalNet.Agents.Shared.Mcp;

/// <summary>Builds an MCP client that authenticates every request as the caller.</summary>
/// <remarks>
/// <para>
/// A factory, not a shared connection: the MCP handshake is itself authenticated, so the client exists only while
/// a call's token does.
/// </para>
/// <para>
/// <b>The transport is stated rather than left to the framework's defaults.</b> A bearer token is attached to every
/// request this client makes, so the three defaults that would undermine that are turned off: a redirect, which
/// would carry the token to an origin the policy never approved; a cookie jar, which is state shared between calls
/// on a transport that exists for one call; and an unchecked certificate revocation list. The connection is pooled
/// for a bounded time because a connection that outlives the token that opened it is worth nothing.
/// </para>
/// <para>
/// <b>The client is handed to the transport, which disposes it.</b> It used to be left unowned and never disposed,
/// so every call leaked a handler and its pool; ownership is stated here because the transport is the only object
/// that knows when the call's last request has been answered.
/// </para>
/// </remarks>
internal static class McpAuthenticationHelper
{
    /// <summary>The authenticated client for one catalogue endpoint.</summary>
    public static async Task<McpClient?> ConnectAsync(
        Uri mcpEndpointUri,
        CatalogueServerRequestPolicy catalogueServerRequestPolicy,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mcpEndpointUri);
        ArgumentNullException.ThrowIfNull(catalogueServerRequestPolicy);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        return await McpClient.CreateAsync(
            new HttpClientTransport(
                new HttpClientTransportOptions
                {
                    Endpoint = mcpEndpointUri,

                    // Named by where it is, so the name cannot say "catalogue" for a deployment pointed
                    // somewhere else.
                    Name = mcpEndpointUri.ToString(),
                },
                HttpClientHelper.CreateWithPolicy(
                    catalogueServerRequestPolicy,
                    loggerFactory
                ),
                loggerFactory,
                ownsHttpClient: true),
            cancellationToken: cancellationToken);
    }

    /// <summary>Compatibility path for tests written before the header reader replaced the token holder.</summary>
    internal static McpAccessTokenHandler BuildTheTokenAttachmentHandler(
        IMcpAccessTokenService tokens,
        CatalogueServerRequestPolicy catalogueServerRequestPolicy,
        ILoggerFactory loggerFactory)
        => new(
            tokens,
            catalogueServerRequestPolicy,
            loggerFactory.CreateLogger<McpAccessTokenHandler>())
        {
            InnerHandler = TerminalHandler(),
        };

    /// <summary>Compatibility path for tests written before the header reader replaced the token holder.</summary>
    internal static HttpClient BuildTheClientThatPresentsTheCallersToken(
        IMcpAccessTokenService tokens,
        CatalogueServerRequestPolicy catalogueServerRequestPolicy,
        ILoggerFactory loggerFactory)
        => new(BuildTheTokenAttachmentHandler(tokens, catalogueServerRequestPolicy, loggerFactory))
        {
            Timeout = TimeSpan.FromMinutes(5),
        };

    private static SocketsHttpHandler TerminalHandler()
        => new()
        {
            UseCookies = false,
            AllowAutoRedirect = false,
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            SslOptions = new System.Net.Security.SslClientAuthenticationOptions
            {
                CertificateRevocationCheckMode = X509RevocationMode.Online,
            },
        };
}
