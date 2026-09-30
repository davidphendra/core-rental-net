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
/// <para>
/// Driven through a real <see cref="McpClient"/> over the test host's own HTTP stack, so the handshake, the
/// tool schema and the JSON-RPC framing are exercised rather than simulated.
/// </para>
/// <para>
/// <b>The tool takes a collection of terms rather than one word, and that is asserted here rather than
/// assumed.</b> No other tool in this application publishes an array parameter, so the schema a model is
/// offered is checked as well as the search — a parameter the model could not fill would leave the whole
/// expansion stage with nowhere to go.
/// </para>
/// </remarks>
public sealed class CatalogMcpTests(CatalogMcpFactory factory) : IClassFixture<CatalogMcpFactory>
{
    private static readonly string[] CatalogueRead = ["read:catalog"];
    private static readonly string[] SimilaritySearch = ["searchsimilarity:aibuilder"];

    /// <summary>One rupiah below the cheapest monitor in the catalogue, so no monitor is affordable.</summary>
    private const decimal BelowEveryMonitorPrice = 86_999m;

    /// <summary>The cheapest monitor in the catalogue.</summary>
    private const decimal CheapestMonitorPrice = 87_000m;

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

    [Fact] // the gate the whole expansion stage rests on: the model must be offered an array
    public async Task The_name_tool_is_offered_several_terms_and_not_one_word()
    {
        await using var client = await ConnectAsync(CatalogueRead);

        var tools = await client.ListToolsAsync();
        var terms = tools.Single(tool => tool.Name == SearchCatalogueTool.ToolName)
            .JsonSchema
            .GetProperty("properties")
            .GetProperty("terms");

        terms.GetProperty("type").GetString().Should().Be("array");
        terms.GetProperty("items").GetProperty("type").GetString().Should().Be("string");

        tools.Single(tool => tool.Name == SearchCatalogueTool.ToolName)
            .JsonSchema
            .GetProperty("required")
            .EnumerateArray()
            .Select(required => required.GetString())
            .Should().Contain("terms");

        tools.Single(tool => tool.Name == SearchCatalogueTool.ToolName)
            .JsonSchema
            .GetProperty("properties")
            .TryGetProperty("search", out _)
            .Should().BeFalse("the one-word parameter was replaced, not joined");
    }

    [Fact] // MCP-04
    public async Task The_name_tool_answers_with_the_compact_envelope_the_rest_api_publishes()
    {
        await using var client = await ConnectAsync(CatalogueRead);

        var answer = await client.CallToolAsync(
            SearchCatalogueTool.ToolName,
            new Dictionary<string, object?>
            {
                ["terms"] = new[] { "monitor" },
                ["subCategory"] = "monitor",
                ["limit"] = 2,
            });

        using var document = JsonDocument.Parse(Text(answer));
        var body = document.RootElement;
        var matches = body.GetProperty("matches");

        // A null number is absent rather than published as null: the SDK omits null members, and "the ceiling
        // excluded nothing" is the absence of a fact rather than a fact with no value. The other test below
        // pins the case where there is something to report.
        body.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(["matches"]);
        matches.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(
            ["value", "count", "total", "truncated", "currency"]);
        matches.GetProperty("currency").GetString().Should().Be("IDR");
        matches.GetProperty("count").GetInt32().Should().BeLessThanOrEqualTo(2);
        matches.GetProperty("value")[0].GetProperty("currency").GetString().Should().Be("IDR");
        matches.GetProperty("value")[0].TryGetProperty("metadata", out _).Should().BeFalse();
    }

    [Fact] // the defect this change exists for: one word the catalogue does not use must not empty the answer
    public async Task Terms_are_searched_as_alternatives_so_a_term_that_matches_nothing_is_survivable()
    {
        await using var client = await ConnectAsync(CatalogueRead);

        var answer = await client.CallToolAsync(
            SearchCatalogueTool.ToolName,
            new Dictionary<string, object?> { ["terms"] = new[] { "workstation", "computer table" } });

        using var document = JsonDocument.Parse(Text(answer));

        document.RootElement.GetProperty("matches").GetProperty("total").GetInt32()
            .Should().BeGreaterThan(0, "'computer table' matches nothing, and 'workstation' must still be found");
    }

    [Fact] // a term is a phrase, so its own words are all required
    public async Task Every_word_of_one_term_must_appear()
    {
        await using var client = await ConnectAsync(CatalogueRead);

        var answer = await client.CallToolAsync(
            SearchCatalogueTool.ToolName,
            new Dictionary<string, object?> { ["terms"] = new[] { "workstation submarine" } });

        using var document = JsonDocument.Parse(Text(answer));

        document.RootElement.GetProperty("matches").GetProperty("total").GetInt32().Should().Be(0);
    }

    [Fact] // the boundary refuses rather than trimming, so the caller learns the limit it broke
    public async Task More_terms_than_the_bound_are_refused()
    {
        await using var client = await ConnectAsync(CatalogueRead);

        var tooManyTerms = Enumerable
            .Range(1, CatalogToolLimits.MaximumSearchTermCount + 1)
            .Select(position => $"term{position}")
            .ToArray();

        var answer = await client.CallToolAsync(
            SearchCatalogueTool.ToolName,
            new Dictionary<string, object?> { ["terms"] = tooManyTerms });

        // A tool's own refusal is an error answer rather than a protocol failure: the call reached the tool,
        // and the tool is the thing saying no. What matters is that it refuses instead of searching with eight.
        // Its sentence does not cross to the caller — the MCP server answers a thrown tool with a line of its
        // own — which is why the bound is taught by the prompt and enforced by the agent's own policy rather
        // than learnt from this refusal.
        answer.IsError.Should().Be(true);
    }

    [Fact]
    public async Task An_empty_term_list_is_refused()
    {
        await using var client = await ConnectAsync(CatalogueRead);

        var answer = await client.CallToolAsync(
            SearchCatalogueTool.ToolName,
            new Dictionary<string, object?> { ["terms"] = Array.Empty<string>() });

        answer.IsError.Should().Be(true);
    }

    [Fact] // a budget that excludes everything is answered with the number the next attempt needs
    public async Task A_ceiling_that_excludes_every_match_reports_the_cheapest_product()
    {
        await using var client = await ConnectAsync(CatalogueRead);

        var answer = await client.CallToolAsync(
            SearchCatalogueTool.ToolName,
            new Dictionary<string, object?>
            {
                ["terms"] = new[] { "monitor" },
                ["subCategory"] = "monitor",
                ["maximumMonthlyAmount"] = BelowEveryMonitorPrice,
            });

        using var document = JsonDocument.Parse(Text(answer));
        var body = document.RootElement;

        body.GetProperty("matches").GetProperty("total").GetInt32().Should().Be(0);
        body.GetProperty("cheapestProductIgnoringTheCeiling").GetDecimal()
            .Should().Be(CheapestMonitorPrice, "an empty answer the ceiling caused is fixable, and this is what fixes it");
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
