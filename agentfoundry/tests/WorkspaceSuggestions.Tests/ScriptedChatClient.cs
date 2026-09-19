using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace WorkspaceSuggestions.Tests;

/// <summary>
/// A chat client that answers with a scripted reply per call, and remembers what it was asked.
/// </summary>
/// <remarks>
/// The workflow gives the rephraser and the suggestor the <b>same</b> client in these tests, so the order of
/// the script is the order of the calls: the first reply is the rephraser's specification, the second is the
/// suggestor's result. That is what makes "it ran in the right order" observable without a model.
/// </remarks>
internal sealed class ScriptedChatClient(params string[] replies) : IChatClient
{
    private int _call;

    /// <summary>What each call was asked, in order. Index 0 is the rephraser, index 1 the suggestor.</summary>
    public List<string> Requests { get; } = [];

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Remember(messages);

        await Task.Yield();

        yield return new ChatResponseUpdate(ChatRole.Assistant, Next());
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        Remember(messages);

        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, Next())));
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }

    private void Remember(IEnumerable<ChatMessage> messages)
        => Requests.Add(string.Join("\n", messages.Select(message => message.Text)));

    /// <summary>The next reply, repeating the last one if the workflow calls more times than scripted.</summary>
    private string Next()
    {
        var reply = replies[Math.Min(_call, replies.Length - 1)];
        _call++;

        return reply;
    }
}
