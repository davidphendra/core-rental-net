using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace CoreRentalNet.Host.Tests;

/// <summary>A model client whose customer stops the run halfway through an answer.</summary>
/// <remarks>
/// It cancels the token that was handed to it and then obeys it, which is what a real transport does when the
/// browser aborts the stream. Written this way rather than by pre-cancelling, because a token that was already
/// cancelled when the run started proves nothing about what happens when it is cancelled <em>during</em> one.
/// </remarks>
internal sealed class StoppingChatClient(CancellationTokenSource run) : IChatClient
{
    private const string HalfAnAnswer = """{ "status": "suggested", "reason": null, """;

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        yield return new ChatResponseUpdate(ChatRole.Assistant, HalfAnAnswer);

        run.Cancel();

        cancellationToken.ThrowIfCancellationRequested();
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("The endpoint streams; this client is only ever asked to stream.");

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
