using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;

namespace CoreRentalNet.Agents.Shared.Mcp;

internal interface IMcpAuthorizationConnection : IAsyncDisposable
{
    /// <summary>True when this deployment has been told where the server is.</summary>
    bool IsConfigured { get; }

    /// <summary>The tools the call's own token entitles, discovered once and kept for the call.</summary>
    Task<IReadOnlyList<AITool>> ToolsAsync(CancellationToken cancellationToken);

    /// <summary>Closes the call's session while its token is still held, and is harmless a second time.</summary>
    ValueTask CloseAsync();
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
internal sealed class McpAuthorizationConnection : IMcpAuthorizationConnection
{
    private readonly McpSetting _mcpSetting;
    private readonly IAccessTokenHeaderReader _accessTokenHeaderReader;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<McpAuthorizationConnection> _logger;
    private McpClient? _client;
    private IReadOnlyList<AITool>? _tools;
    private bool _closed;

    [ActivatorUtilitiesConstructor]
    public McpAuthorizationConnection(
        McpSetting mcpSetting,
        IAccessTokenHeaderReader accessTokenHeaderReader,
        ILoggerFactory loggerFactory,
        ILogger<McpAuthorizationConnection> logger)
    {
        _mcpSetting = mcpSetting;
        _accessTokenHeaderReader = accessTokenHeaderReader;
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    /// <summary>Compatibility path for tests written before the header reader replaced the token holder.</summary>
    public McpAuthorizationConnection(
        McpSetting mcpSetting,
        IMcpAccessTokenService tokens,
        ILoggerFactory loggerFactory,
        ILogger<McpAuthorizationConnection> logger)
        : this(mcpSetting, new TokenServiceHeaderReader(tokens), loggerFactory, logger)
    {
    }

    /// <summary>True when this deployment has been told where the server is.</summary>
    public bool IsConfigured => _mcpSetting.IsConfigured;

    /// <summary>The tools the call's own token entitles, discovered once and kept for the call.</summary>
    public async Task<IReadOnlyList<AITool>> ToolsAsync(CancellationToken cancellationToken)
    {
        if (!_mcpSetting.IsConfigured)
        {
            return [];
        }

        if (_tools is not null)
        {
            return _tools;
        }

        McpRunContext? previous = McpRunScope.Current;

        try
        {
            var accessToken = _accessTokenHeaderReader.ReadCallerAccessToken() ?? string.Empty;
            McpRunScope.Current = new McpRunContext(AccessTokenHeader.Name, accessToken);

            _client = await McpAuthenticationHelper.ConnectAsync(
                new Uri(_mcpSetting.McpEndpoint, UriKind.Absolute),
                // Built here rather than in the constructor: an unconfigured deployment has no endpoint to
                // describe, and this is the point past which one is required.
                new CatalogueServerRequestPolicy(_mcpSetting),
                _loggerFactory,
                cancellationToken);

            _tools = _client is null
                ? []
                : [.. (await _client.ListToolsAsync(cancellationToken: cancellationToken)).Cast<AITool>()];

            _logger.LogInformation(
                "MCP server {Endpoint}: {Count} tool(s) available to this call.",
                _mcpSetting.McpEndpoint,
                _tools.Count);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The caller cannot see why a run ended, and the application deliberately drops the agent's reason,
            // so the one place it survives is here. The token is never logged, only the endpoint and the fault.
            _logger.LogWarning(
                exception,
                "The MCP server at {Endpoint} refused this call's tools.",
                _mcpSetting.McpEndpoint);

            // Typed rather than raw: the feature ends the run Unavailable when the catalogue refuses it, and a
            // transport exception would escape the workflow as a failed run instead.
            throw new CatalogueUnavailableException(_mcpSetting.McpEndpoint, exception);
        }
        finally
        {
            McpRunScope.Current = previous;
        }

        return _tools;
    }

    /// <summary>Closes the session, which is the one authenticated thing the ending must do before releasing.</summary>
    /// <remarks>
    /// The MCP transport sends its session-termination request from inside the client's disposal, and that request
    /// carries the caller's token. Closing here, while the token is still held, is what lets the transport clean
    /// up; the scope's own disposal then finds the work already done.
    /// </remarks>
    public async ValueTask CloseAsync()
    {
        if (_closed)
        {
            return;
        }

        _closed = true;
        _tools = null;

        if (_client is not null)
        {
            await _client.DisposeAsync();
            _client = null;
        }
    }

    public ValueTask DisposeAsync() => CloseAsync();
}
