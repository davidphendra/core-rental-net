using AwesomeAssertions;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Agents;
using CoreRentalNet.Agents.Shared.ChatClients;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

public sealed class CatalogueProjectionChainTests
{
    [Fact]
    public async Task The_tool_then_json_final_call_is_projected()
    {
        var inner = new FakeTelemetryChatClient();
        var projection = new CatalogueToolResultProjectionChatClient(inner);
        var client = new ToolThenJsonChatClient(
            projection,
            ChatResponseFormat.Text,
            NullLogger<ToolThenJsonChatClient>.Instance);

        await client.GetResponseAsync(
            [
                new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("call-1", "search_catalogue")]),
                new ChatMessage(ChatRole.Tool, [new FunctionResultContent(
                    "call-1",
                    """{"matches":{"value":[{"sku":"X"}],"count":1,"total":1,"truncated":false}}""")]),
            ],
            new ChatOptions
            {
                Tools = [AIFunctionFactory.Create(() => "{}", "search_catalogue")],
                ToolMode = ChatToolMode.Auto,
            });

        var result = inner.LastMessages
            .SelectMany(message => message.Contents)
            .OfType<FunctionResultContent>()
            .Single()
            .Result!
            .ToString()!;

        result.Should().NotContain("X");
    }
}
