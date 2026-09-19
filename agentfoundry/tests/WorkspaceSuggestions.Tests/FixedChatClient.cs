using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace WorkspaceSuggestions.Tests;

/// <summary>A chat client that answers with a fixed string. No network, no model, no credential.</summary>
/// <remarks>
/// Hand-written rather than mocked, as this repository does everywhere else. Both shapes are
/// implemented because the hosted <b>Responses</b> path streams: it calls the streaming member, and a fake
/// that refused to stream would make every host test fail with a server error rather than answer.
/// </remarks>
internal sealed class FixedChatClient(string reply) : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, reply)));

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        yield return new ChatResponseUpdate(ChatRole.Assistant, reply);
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
