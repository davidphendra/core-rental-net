using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

namespace CoreRentalNet.Agents.Shared.Mcp;

internal interface IMcpAuthorizationConnection
{
    /// <summary>True when this deployment has been told where the server is.</summary>
    bool IsConfigured { get; }

    /// <summary>The tools the call's own token entitles, discovered once and kept for the call.</summary>
    Task<IReadOnlyList<AITool>> ToolsAsync(CancellationToken cancellationToken);
}

/// <summary>One call's connection to an MCP server, and the tools its token entitles.</summary>
/// <remarks>
/// <para>
/// The MCP client has to outlive the listing that produced the tools: a tool hands its calls back through the
/// client it came from, so disposing the client as soon as <c>tools/list</c> returns leaves every tool call on a
/// dead session. The connection is therefore scoped to the call, exactly as long as the token it presents.
/// </para>
/// <para>
/// One connection per call, never shared: the handshake is authenticated as the caller, so a connection carried
/// between calls would present one customer's token to the server for another's run.
/// </para>
/// </remarks>
internal sealed class McpAuthorizationConnection(
    McpSetting mcpSetting,
    IMcpAccessTokenService accessTokenService,
    ILoggerFactory loggerFactory,
    ILogger<McpAuthorizationConnection> logger) : IMcpAuthorizationConnection, IAsyncDisposable
{
    private McpClient? _client;
    private IReadOnlyList<AITool>? _tools;

    /// <summary>True when this deployment has been told where the server is.</summary>
    public bool IsConfigured => mcpSetting.IsConfigured;

    /// <summary>The tools the call's own token entitles, discovered once and kept for the call.</summary>
    public async Task<IReadOnlyList<AITool>> ToolsAsync(CancellationToken cancellationToken)
    {
        if (!mcpSetting.IsConfigured)
        {
            return [];
        }

        if (_tools is not null)
        {
            return _tools;
        }

        try
        {
            _client = await McpAuthenticationHelper.ConnectAsync(
                new Uri(mcpSetting.McpEndpoint, UriKind.Absolute),
                accessTokenService,

                // Built here rather than in the constructor: an unconfigured deployment has no endpoint to
                // describe, and this is the point past which one is required.
                new CatalogueServerRequestPolicy(mcpSetting),
                loggerFactory,
                cancellationToken);

            _tools = _client is null
                ? []
                : [.. (await _client.ListToolsAsync(cancellationToken: cancellationToken)).Cast<AITool>()];

            logger.LogInformation(
                "MCP server {Endpoint}: {Count} tool(s) available to this call.",
                mcpSetting.McpEndpoint,
                _tools.Count);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The caller cannot see why a run ended, and the application deliberately drops the agent's reason,
            // so the one place it survives is here. The token is never logged, only the endpoint and the fault.
            logger.LogWarning(
                exception,
                "The MCP server at {Endpoint} refused this call's tools.",
                mcpSetting.McpEndpoint);

            throw;
        }

        return _tools;
    }

    public async ValueTask DisposeAsync()
    {
        if (_client is not null)
        {
            await _client.DisposeAsync();
        }
    }
}
