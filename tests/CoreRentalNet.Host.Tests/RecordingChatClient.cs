using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace CoreRentalNet.Host.Tests;

/// <summary>A chat client that answers with a fixed reply and remembers what it was asked.</summary>
/// <remarks>
/// Hand-written, as this repository does everywhere else, and it records its prompts so a test can ask what
/// the adapter actually sent — which is how "no state is carried between runs" becomes observable rather than
/// asserted in a comment.
/// </remarks>
internal sealed class RecordingChatClient(string reply) : IChatClient
{
    public List<string> Prompts { get; } = [];

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Remember(messages);

        await Task.Yield();

        yield return new ChatResponseUpdate(ChatRole.Assistant, reply);
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        Remember(messages);

        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, reply)));
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }

    private void Remember(IEnumerable<ChatMessage> messages)
        => Prompts.Add(string.Join("\n", messages.Select(message => message.Text)));
}
