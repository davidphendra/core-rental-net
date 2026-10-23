using System.Text.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Agents;
using CoreRentalNet.Agents.Shared.ChatClients;
using Microsoft.Extensions.AI;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>The retriever's final model call receives search summaries, not the product rows.</summary>
public sealed class CatalogueToolResultProjectionChatClientTests
{
    private const string FullAnswer = """
        {
          "matches": {
            "value": [
              {
                "sku": "DSK-1",
                "name": "Desk",
                "description": "A desk."
              }
            ],
            "count": 1,
            "total": 2,
            "truncated": true
          },
          "cheapestProductIgnoringTheCeiling": 100000
        }
        """;

    [Fact]
    public async Task The_final_structured_call_receives_only_the_search_summary()
    {
        var inner = new FakeTelemetryChatClient();
        var client = new CatalogueToolResultProjectionChatClient(inner);

        await client.GetResponseAsync(ToolMessages(), Options(ChatCallPhase.FinalStructured));

        var resultText = inner.LastMessages
            .SelectMany(message => message.Contents)
            .OfType<FunctionResultContent>()
            .Single()
            .Result!
            .ToString()!;
        using var document = JsonDocument.Parse(resultText);
        var root = document.RootElement;

        root.GetProperty("matches").TryGetProperty("value", out _).Should().BeFalse();
        root.GetProperty("matches").GetProperty("count").GetInt32().Should().Be(1);
        root.GetProperty("matches").GetProperty("total").GetInt32().Should().Be(2);
        root.GetProperty("matches").GetProperty("truncated").GetBoolean().Should().BeTrue();
        root.GetProperty("cheapestProductIgnoringTheCeiling").GetDecimal().Should().Be(100000m);
    }

    [Fact]
    public async Task The_tool_planning_call_is_not_projected()
    {
        var inner = new FakeTelemetryChatClient();
        var client = new CatalogueToolResultProjectionChatClient(inner);

        await client.GetResponseAsync(ToolMessages(), Options(ChatCallPhase.ToolPlanning));

        var resultText = inner.LastMessages
            .SelectMany(message => message.Contents)
            .OfType<FunctionResultContent>()
            .Single()
            .Result!
            .ToString()!;

        resultText.Should().Be(FullAnswer);
    }

    [Fact]
    public async Task A_text_content_result_is_projected()
    {
        var inner = new FakeTelemetryChatClient();
        var client = new CatalogueToolResultProjectionChatClient(inner);

        await client.GetResponseAsync(
            [
                new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("call-1", "search_catalogue")]),
                new ChatMessage(ChatRole.Tool, [new FunctionResultContent("call-1", new TextContent(FullAnswer))]),
            ],
            Options(ChatCallPhase.FinalStructured));

        var text = inner.LastMessages
            .SelectMany(message => message.Contents)
            .OfType<FunctionResultContent>()
            .Single()
            .Result!
            .ToString()!;

        using var document = JsonDocument.Parse(text);
        document.RootElement.GetProperty("matches").TryGetProperty("value", out _).Should().BeFalse();
    }

    [Fact]
    public async Task A_json_node_result_is_projected()
    {
        var inner = new FakeTelemetryChatClient();
        var client = new CatalogueToolResultProjectionChatClient(inner);
        var result = JsonNode.Parse(FullAnswer)!;

        await client.GetResponseAsync(
            [
                new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("call-1", "search_catalogue")]),
                new ChatMessage(ChatRole.Tool, [new FunctionResultContent("call-1", result)]),
            ],
            Options(ChatCallPhase.FinalStructured));

        var text = inner.LastMessages
            .SelectMany(message => message.Contents)
            .OfType<FunctionResultContent>()
            .Single()
            .Result!
            .ToString()!;

        using var document = JsonDocument.Parse(text);
        document.RootElement.GetProperty("matches").TryGetProperty("value", out _).Should().BeFalse();
    }

    [Fact]
    public async Task A_non_catalogue_result_is_left_unchanged()
    {
        var inner = new FakeTelemetryChatClient();
        var client = new CatalogueToolResultProjectionChatClient(inner);
        const string other = "{\"answer\":42}";

        await client.GetResponseAsync(
            [new ChatMessage(ChatRole.Tool, [new FunctionResultContent("call-1", other)])],
            Options(ChatCallPhase.FinalStructured));

        inner.LastMessages
            .SelectMany(message => message.Contents)
            .OfType<FunctionResultContent>()
            .Single()
            .Result!
            .ToString()
            .Should().Be(other);
    }

    private static IReadOnlyList<ChatMessage> ToolMessages()
        =>
        [
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("call-1", "search_catalogue")]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("call-1", FullAnswer)]),
        ];

    private static ChatOptions Options(string phase)
        => new()
        {
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                [ChatCallPhase.Key] = phase,
            },
        };
}
