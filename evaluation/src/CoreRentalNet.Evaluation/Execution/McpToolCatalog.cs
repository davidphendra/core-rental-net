using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Client;

namespace CoreRentalNet.Evaluation.Execution;

/// <summary>Lists the application's MCP tools with the caller's token.</summary>
public sealed class McpToolCatalog(Uri endpoint, string callerToken) : IToolCatalog
{
    private const string CallerTokenHeader = "x-client-mcp-catalog-access-token";

    public async Task<IReadOnlyList<AITool>> ListAsync(CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation(CallerTokenHeader, callerToken);

        var transport = new HttpClientTransport(
            new HttpClientTransportOptions { Endpoint = endpoint },
            httpClient,
            NullLoggerFactory.Instance,
            ownsHttpClient: false);

        await using var client = await McpClient.CreateAsync(
            transport,
            cancellationToken: cancellationToken);

        return [.. (await client.ListToolsAsync(cancellationToken: cancellationToken)).Cast<AITool>()];
    }
}
