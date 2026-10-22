using AwesomeAssertions;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;
using CoreRentalNet.Agents.Shared.ChatClients;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>
/// The retriever needs two request shapes: tools with no response grammar, then the response grammar with no
/// tools. Some local model servers reject the combination of both in one request.
/// </summary>
public sealed class ToolThenJsonChatClientTests
{
    private static readonly ChatResponseFormat RetrievalFormat =
        ChatResponseFormat.ForJsonSchema<CatalogueProductRetrievalResult>();

    [Fact]
    public async Task The_tool_call_has_tools_and_no_response_format()
    {
        var inner = new CapturingChatClient();
        var client = ClientOver(inner);

        await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "find desks")],
            Options());

        inner.LastOptions.Should().NotBeNull();
        inner.LastOptions!.Tools.Should().ContainSingle();
        inner.LastOptions.ToolMode.Should().Be(ChatToolMode.Auto);
        inner.LastOptions.ResponseFormat.Should().BeNull();
    }

    [Fact]
    public async Task The_structured_call_has_no_tools_and_the_response_format()
    {
        var inner = new CapturingChatClient();
        var client = ClientOver(inner);

        await client.GetResponseAsync(ToolResults(), Options());

        inner.LastOptions.Should().NotBeNull();
        inner.LastOptions!.Tools.Should().BeEmpty();
        inner.LastOptions.ToolMode.Should().BeNull();
        inner.LastOptions.ResponseFormat.Should().BeSameAs(RetrievalFormat);
    }

    [Fact]
    public async Task The_streaming_call_uses_the_same_two_shapes()
    {
        var inner = new CapturingChatClient();
        var client = ClientOver(inner);

        await foreach (var _ in client.GetStreamingResponseAsync(
            [new ChatMessage(ChatRole.User, "find desks")],
            Options()))
        {
        }

        inner.LastOptions!.Tools.Should().ContainSingle();
        inner.LastOptions.ResponseFormat.Should().BeNull();

        await foreach (var _ in client.GetStreamingResponseAsync(ToolResults(), Options()))
        {
        }

        inner.LastOptions!.Tools.Should().BeEmpty();
        inner.LastOptions.ResponseFormat.Should().BeSameAs(RetrievalFormat);
    }

    [Fact]
    public async Task A_call_with_no_tools_keeps_the_response_format()
    {
        var inner = new CapturingChatClient();
        var client = ClientOver(inner);

        await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "find desks")],
            new ChatOptions { ResponseFormat = RetrievalFormat });

        inner.LastOptions!.Tools.Should().BeNullOrEmpty();
        inner.LastOptions.ResponseFormat.Should().BeSameAs(RetrievalFormat);
    }

    [Fact]
    public async Task The_callers_options_are_not_changed()
    {
        var inner = new CapturingChatClient();
        var options = Options();

        await ClientOver(inner).GetResponseAsync(
            [new ChatMessage(ChatRole.User, "find desks")],
            options);

        options.Tools.Should().ContainSingle();
        options.ToolMode.Should().Be(ChatToolMode.Auto);
        options.ResponseFormat.Should().BeSameAs(RetrievalFormat);
    }

    private static ToolThenJsonChatClient ClientOver(IChatClient inner)
        => new(inner, RetrievalFormat, NullLogger<ToolThenJsonChatClient>.Instance);

    private static ChatOptions Options()
        => new()
        {
            Tools = [AIFunctionFactory.Create(() => "{}", "search_catalogue")],
            ToolMode = ChatToolMode.Auto,
            ResponseFormat = RetrievalFormat,
        };

    private static IReadOnlyList<ChatMessage> ToolResults()
        =>
        [
            new(
                ChatRole.Assistant,
                [new FunctionCallContent("call-1", "search_catalogue")]),
            new(
                ChatRole.Tool,
                [new FunctionResultContent("call-1", "{\"matches\":[]}")]),
        ];
}
