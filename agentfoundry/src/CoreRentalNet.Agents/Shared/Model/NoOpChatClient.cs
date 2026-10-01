using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace CoreRentalNet.Agents.Shared.Model;

/// <summary>A chat client that answers nothing, for a client whose reply does not come from a model.</summary>
/// <remarks>
/// The echo stage's client is deterministic: it replies with its caller's own message and never consults a model.
/// It still needs an inner client to be a <see cref="DelegatingChatClient"/>, and this is that inner — a leaf that
/// produces no reply of its own, so a client that does consult it answers nothing rather than reaching a model.
/// One instance is shared, because it holds nothing.
/// </remarks>
internal sealed class NoOpChatClient : IChatClient
{
    /// <summary>The one instance, because the client holds no state.</summary>
    public static NoOpChatClient Instance { get; } = new();

    private NoOpChatClient()
    {
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new ChatResponse());

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        yield break;
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
