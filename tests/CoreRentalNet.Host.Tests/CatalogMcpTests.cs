using System.Text.Json;
using AwesomeAssertions;
using CoreRentalNet.Host.Configs;
using CoreRentalNet.Host.Mcp;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The catalogue's MCP surface: what a caller discovers, what it may call, and the shape of the answer.
/// </summary>
/// <remarks>
/// Driven through a real <see cref="McpClient"/> over the test host's own HTTP stack, so the handshake, the
/// tool schema and the JSON-RPC framing are exercised rather than simulated.
/// </remarks>
public sealed class CatalogMcpTests(CatalogMcpFactory factory) : IClassFixture<CatalogMcpFactory>
{
    private static readonly string[] CatalogueRead = ["read:catalog"];
    private static readonly string[] SimilaritySearch = ["searchsimilarity:aibuilder"];

    [Fact] // MCP-01
    public async Task A_caller_discovers_only_the_tools_its_token_entitles_it_to()
    {
        await using var client = await ConnectAsync(CatalogueRead);

        var tools = await client.ListToolsAsync();

        tools.Select(tool => tool.Name).Should().Equal(SearchCatalogueTool.ToolName);
    }

    [Fact] // MCP-01
    public async Task The_other_permission_discovers_the_other_tool()
    {
        await using var client = await ConnectAsync(SimilaritySearch);

        var tools = await client.ListToolsAsync();

        tools.Select(tool => tool.Name).Should().Equal(SearchSimilarityCatalogueTool.ToolName);
    }

    [Fact] // MCP-02
    public async Task A_tool_the_caller_was_not_shown_is_refused_when_called()
    {
        await using var client = await ConnectAsync(CatalogueRead);

        var act = () => client.CallToolAsync(
            SearchSimilarityCatalogueTool.ToolName,
            new Dictionary<string, object?> { ["query"] = "two screens" }).AsTask();

        await act.Should().ThrowAsync<McpProtocolException>();
    }

    [Fact] // MCP-04
    public async Task The_name_tool_answers_with_the_compact_envelope_the_rest_api_publishes()
    {
        await using var client = await ConnectAsync(CatalogueRead);

        var answer = await client.CallToolAsync(
            SearchCatalogueTool.ToolName,
            new Dictionary<string, object?> { ["search"] = "a", ["subCategory"] = "monitor", ["limit"] = 2 });

        using var document = JsonDocument.Parse(Text(answer));
        var body = document.RootElement;

        body.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(
            ["value", "count", "total", "truncated", "currency"]);
        body.GetProperty("currency").GetString().Should().Be("IDR");
        body.GetProperty("count").GetInt32().Should().BeLessThanOrEqualTo(2);
        body.GetProperty("value")[0].GetProperty("currency").GetString().Should().Be("IDR");
        body.GetProperty("value")[0].TryGetProperty("metadata", out _).Should().BeFalse();
    }

    [Fact] // MCP-02
    public async Task Without_a_token_the_endpoint_refuses_the_handshake()
    {
        var act = () => ConnectAsync();

        await act.Should().ThrowAsync<Exception>("the MCP endpoint requires an authenticated caller");
    }

    private async Task<McpClient> ConnectAsync(params string[] permissions)
    {
        var http = factory.CreateClient();

        if (permissions.Length > 0)
        {
            http.DefaultRequestHeaders.Add(CatalogApiTestHandler.PermissionsHeader, string.Join(',', permissions));
        }

        return await McpClient.CreateAsync(new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(http.BaseAddress!, McpRoutes.Path),
                Name = "catalogue",
            },
            http,
            null,
            false));
    }

    /// <summary>Whatever the tool answered with: its text, or the structured content when the text is empty.</summary>
    private static string Text(CallToolResult result)
    {
        var text = string.Concat(result.Content.OfType<TextContentBlock>().Select(block => block.Text));

        return string.IsNullOrEmpty(text) ? result.StructuredContent?.ToString() ?? string.Empty : text;
    }
}
