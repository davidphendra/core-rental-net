using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace CoreRentalNet.Agents.Features.EchoReply.ChatClient;

/// <summary>The echo stage's client: it answers with the caller's own message.</summary>
/// <remarks>
/// <para>
/// The deterministic counterpart of the model client. Every other stage's client is a decorated Foundry client;
/// this one <b>is</b> the stage, so a run through it costs nothing, needs no credential and cannot fail on a
/// network. It is the only part of this feature that is not shared with the pipeline.
/// </para>
/// <para>
/// It derives from <see cref="DelegatingChatClient"/> so it has the same shape as every other client in the
/// pipeline, but it answers without consulting its inner client: the reply is deterministic, so there is nothing
/// for the inner to contribute. The inner is supplied by the composition and is a leaf that answers nothing.
/// </para>
/// <para>
/// It answers with the last user message, which is the query a console sent. The profile's prompt is the
/// contract this client implements — repeated verbatim, nothing added — and a test asserts that it does, because
/// a deterministic client is not bound by a prompt the way a model is.
/// </para>
/// </remarks>
internal sealed class EchoReplyChatClient(IChatClient innerClient) : DelegatingChatClient(innerClient)
{
    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, EchoOf(messages))));

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        yield return new ChatResponseUpdate(ChatRole.Assistant, EchoOf(messages));
    }

    /// <summary>The caller's message, which is the last user message the stage was handed.</summary>
    private static string EchoOf(IEnumerable<ChatMessage> messages)
        => messages.LastOrDefault(message => message.Role == ChatRole.User)?.Text ?? string.Empty;
}
