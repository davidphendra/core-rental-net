using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace CoreRentalNet.Agents.Tests;

/// <summary>
/// A chat client that answers with a scripted reply per call, and remembers what it was asked.
/// </summary>
/// <remarks>
/// <para>
/// The workflow gives every stage the <b>same</b> client in these tests, so the order of the script is the order of
/// the calls — which is what makes "it ran in the right order" observable without a model.
/// </para>
/// <para>
/// <b><see cref="AfterEachReply"/> exists because a scripted client calls no tools.</b> The products a run composes
/// from now come from the recorded tool answers rather than from the retriever's own reply, so a test that needs a
/// non-empty pool records an answer here — at the moment the answer would have come back from a tool.
/// </para>
/// <para>
/// It is a <b>model</b> double, not coverage of the recording seam. That seam — a tool offered by the client,
/// invoked by the function loop, and run through the guardrail pipeline into the ledger — is driven for real by
/// <c>CatalogueToolGuardrailTests</c>, so a stage test can keep scripting its model without hiding the path a
/// tool actually takes.
/// </para>
/// </remarks>
internal sealed class ScriptedChatClient(params string[] replies) : IChatClient
{
    private int _call;

    /// <summary>What each call was asked, in order. Index 0 is the first stage to run.</summary>
    public List<string> Requests { get; } = [];

    /// <summary>The options each call carried, in the same order. Null when the call carried none.</summary>
    public List<ChatOptions?> Options { get; } = [];

    /// <summary>Run after each reply is produced, for a test that needs the run to have a tool answer.</summary>
    public Action? AfterEachReply { get; set; }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Remember(messages, options);

        await Task.Yield();

        yield return new ChatResponseUpdate(ChatRole.Assistant, Next());
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        Remember(messages, options);

        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, Next())));
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }

    private void Remember(IEnumerable<ChatMessage> messages, ChatOptions? options)
    {
        Requests.Add(string.Join("\n", messages.Select(message => message.Text)));
        Options.Add(options);
    }

    /// <summary>The next reply, repeating the last one if the workflow calls more times than scripted.</summary>
    private string Next()
    {
        var reply = replies[Math.Min(_call, replies.Length - 1)];
        _call++;

        AfterEachReply?.Invoke();

        return reply;
    }
}
