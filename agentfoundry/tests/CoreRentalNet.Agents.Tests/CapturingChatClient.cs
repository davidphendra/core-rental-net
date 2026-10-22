using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace CoreRentalNet.Agents.Tests;

/// <summary>Captures the options each model call carries.</summary>
internal sealed class CapturingChatClient : IChatClient
{
    public ChatOptions? LastOptions { get; private set; }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        LastOptions = options;

        return Task.FromResult(new ChatResponse(
            new ChatMessage(ChatRole.Assistant, "{}")));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        LastOptions = options;

        await Task.Yield();

        yield return new ChatResponseUpdate(ChatRole.Assistant, "{}");
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
