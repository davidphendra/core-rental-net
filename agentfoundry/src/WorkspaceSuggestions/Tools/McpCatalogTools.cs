using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

namespace WorkspaceSuggestions.Tools;

/// <summary>The catalogue's tools, discovered once from its MCP server and held for the process.</summary>
/// <remarks>
/// <para>
/// Discovery is a handshake; paying it per run would put a network round-trip in front of every suggestion, so
/// the client is created at startup and disposed with the host.
/// </para>
/// <para>
/// <b>An unconfigured deployment has no tools rather than a broken one.</b> Every catalogue setting is required
/// together, so the tools are absent until an operator sets them. A configured server that cannot be reached
/// stops the host instead: an agent that started with no tools would answer every run from memory, which is the
/// failure this arrangement exists to make loud.
/// </para>
/// </remarks>
internal sealed class McpCatalogTools(McpClient? client, IReadOnlyList<AITool> tools) : ICatalogTools
{
    /// <inheritdoc />
    public IReadOnlyList<AITool> Tools { get; } = tools;

    /// <summary>Connects to the catalogue's MCP server and lists what it offers, or nothing when unconfigured.</summary>
    public static async Task<ICatalogTools> ConnectAsync(
        CatalogToolSettings settings,
        ICatalogAccessToken token,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(token);

        if (!settings.IsConfigured)
        {
            return new McpCatalogTools(null, []);
        }

        var http = new HttpClient(new BearerHandler(token));

        var client = await McpClient.CreateAsync(
            new HttpClientTransport(
                new HttpClientTransportOptions
                {
                    Endpoint = new Uri(settings.McpEndpoint, UriKind.Absolute),
                    Name = "catalogue",
                },
                http,
                null,
                false),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var tools = await client.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        return new McpCatalogTools(client, [.. tools.Cast<AITool>()]);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => client is null ? ValueTask.CompletedTask : client.DisposeAsync();
}
