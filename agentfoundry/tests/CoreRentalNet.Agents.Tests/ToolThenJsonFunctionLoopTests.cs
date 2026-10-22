using AwesomeAssertions;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;
using CoreRentalNet.Agents.Shared.ChatClients;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>The function loop, not only the decorator, must see the tool-result call without tools.</summary>
public sealed class ToolThenJsonFunctionLoopTests
{
    private static readonly ChatResponseFormat RetrievalFormat =
        ChatResponseFormat.ForJsonSchema<CatalogueProductRetrievalResult>();

    [Fact]
    public async Task The_function_loop_reaches_the_structured_call_with_the_tool_result()
    {
        var model = new ToolCallingChatClient();
        var client = new FunctionInvokingChatClient(
            new ToolThenJsonChatClient(
                model,
                RetrievalFormat,
                NullLogger<ToolThenJsonChatClient>.Instance),
            NullLoggerFactory.Instance);

        var tool = AIFunctionFactory.Create(
            () => "{\"matches\":[]}",
            "search_catalogue");

        var response = await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "find desks")],
            new ChatOptions
            {
                Tools = [tool],
                ToolMode = ChatToolMode.Auto,
                ResponseFormat = RetrievalFormat,
            });

        model.Calls.Should().HaveCount(2);

        model.Calls[0]!.Tools.Should().ContainSingle();
        model.Calls[0]!.ResponseFormat.Should().BeNull();

        model.Calls[1]!.Tools.Should().BeEmpty();
        model.Calls[1]!.ToolMode.Should().BeNull();
        model.Calls[1]!.ResponseFormat.Should().BeSameAs(RetrievalFormat);

        response.Text.Should().Be("{}");
    }

    private sealed class ToolCallingChatClient : IChatClient
    {
        public List<ChatOptions?> Calls { get; } = [];

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Calls.Add(options);

            if (Calls.Count == 1)
            {
                return Task.FromResult(new ChatResponse(
                    new ChatMessage(
                        ChatRole.Assistant,
                        [new FunctionCallContent("call-1", "search_catalogue")])));
            }

            return Task.FromResult(new ChatResponse(
                new ChatMessage(ChatRole.Assistant, "{}")));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
}
