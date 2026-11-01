using System.Net.Http.Headers;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Client;

using CoreRentalNet.Evaluation.Execution.Transport;
using CoreRentalNet.Evaluation.Execution.Auth;
using CoreRentalNet.Evaluation.Execution.Running;
namespace CoreRentalNet.Evaluation.Execution.Tools;

/// <summary>Lists the application's MCP tools with the caller's token.</summary>
public sealed class McpToolCatalog(Uri endpoint) : IToolCatalog
{
    public async Task<IReadOnlyList<AITool>> ListAsync(
        string callerToken,
        CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", callerToken);

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
