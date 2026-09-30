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
    /// <summary>How long one catalogue call may take before the transport gives up on it.</summary>
    /// <remarks>
    /// The catalogue's search tools can take longer than the 100-second default — a similarity search embeds the
    /// sentence and scans the index — and a cancelled catalogue call is a failed run. The budget is stated rather
    /// than left to the default the transport would otherwise apply.
    /// </remarks>
    private static readonly TimeSpan CallTimeout = TimeSpan.FromMinutes(5);

    /// <summary>How long a pooled connection to the catalogue is kept before it is reopened.</summary>
    private static readonly TimeSpan PooledConnectionLifetime = TimeSpan.FromMinutes(2);

    /// <summary>The authenticated client for one catalogue endpoint.</summary>
    public static async Task<McpClient?> ConnectAsync(
        Uri mcpEndpointUri,
        IMcpAccessTokenService mcpAccessTokenService,
        CatalogueServerRequestPolicy catalogueServerRequestPolicy,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mcpEndpointUri);
        ArgumentNullException.ThrowIfNull(mcpAccessTokenService);
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
                BuildTheClientThatPresentsTheCallersToken(
                    mcpAccessTokenService, catalogueServerRequestPolicy, loggerFactory),
                loggerFactory,
                ownsHttpClient: true),
            cancellationToken: cancellationToken);
    }

    /// <summary>The client the transport is handed: the token handler over the terminal handler it delegates to.</summary>
    /// <remarks>
    /// Stated apart from <see cref="ConnectAsync"/> so what the transport is protected by can be read and asserted
    /// without a network. It is a credential's whole safety on this side: one server, over TLS, with no redirect
    /// and no cookie jar to carry it anywhere else.
    /// </remarks>
    internal static HttpClient BuildTheClientThatPresentsTheCallersToken(
        IMcpAccessTokenService mcpAccessTokenService,
        CatalogueServerRequestPolicy catalogueServerRequestPolicy,
        ILoggerFactory loggerFactory)
        => new(BuildTheTokenAttachmentHandler(mcpAccessTokenService, catalogueServerRequestPolicy, loggerFactory))
        {
            Timeout = CallTimeout,
        };

    /// <summary>The handler that attaches the token, over the terminal handler it delegates to.</summary>
    internal static CallerCatalogueAccessTokenAttachmentHandler BuildTheTokenAttachmentHandler(
        IMcpAccessTokenService mcpAccessTokenService,
        CatalogueServerRequestPolicy catalogueServerRequestPolicy,
        ILoggerFactory loggerFactory)
        // A DelegatingHandler must be given the terminal handler it delegates to, or the first request fails
        // with "The inner handler has not been assigned."
        => new(
            mcpAccessTokenService,
            catalogueServerRequestPolicy,
            loggerFactory.CreateLogger<CallerCatalogueAccessTokenAttachmentHandler>())
        {
            InnerHandler = new SocketsHttpHandler
            {
                UseCookies = false,
                AllowAutoRedirect = false,
                PooledConnectionLifetime = PooledConnectionLifetime,

                // The MAF sample states this as HttpClientHandler.CheckCertificateRevocationList;
                // SocketsHttpHandler — which pools better, and is what this transport uses — carries the same
                // setting on its TLS options, and its default is to check nothing.
                SslOptions = new System.Net.Security.SslClientAuthenticationOptions
                {
                    CertificateRevocationCheckMode = X509RevocationMode.Online,
                },
            },
        };
}
