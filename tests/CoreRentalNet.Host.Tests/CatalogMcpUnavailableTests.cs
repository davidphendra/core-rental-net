using AwesomeAssertions;
using CoreRentalNet.Host.Mcp;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// A tool whose index cannot be read answers with the model-safe message and leaks nothing else.
/// </summary>
/// <remarks>
/// The vector file's path is written for whoever runs the ingestion tool. The REST endpoint keeps it out of its
/// body; a tool answer is spent in a model's context and a transcript, so the same rule applies harder.
/// </remarks>
public sealed class CatalogMcpUnavailableTests(CatalogMcpUnavailableFactory factory)
    : IClassFixture<CatalogMcpUnavailableFactory>
{
    [Fact] // MCP-05
    public async Task An_unusable_index_is_refused_without_naming_where_the_file_would_be()
    {
        var http = factory.CreateClient();
        http.DefaultRequestHeaders.Add(CatalogApiTestHandler.PermissionsHeader, "searchsimilarity:aibuilder");

        await using var client = await McpClient.CreateAsync(new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(http.BaseAddress!, CoreRentalNet.Host.Configs.McpRoutes.Path),
                Name = "catalogue",
            },
            http,
            null,
            false));

        CallToolResult answer;

        try
        {
            answer = await client.CallToolAsync(
                SearchSimilarityCatalogueTool.ToolName,
                new Dictionary<string, object?> { ["query"] = "two screens" });
        }
        catch (McpProtocolException exception)
        {
            // A transport that refuses the call outright still must not carry the path.
            exception.Message.Should().NotContain("product_embedding").And.NotContain("/tmp/secret");

            return;
        }

        answer.IsError.Should().BeTrue();

        // The server answers a thrown tool with a generic line of its own ("An error occurred invoking …"),
        // so what matters is what must NOT be there: the operator's reason, path and all.
        var text = string.Concat(answer.Content.OfType<TextContentBlock>().Select(block => block.Text));

        text.Should().NotContain("product_embedding");
        text.Should().NotContain("/tmp/secret");
    }
}
