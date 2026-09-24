using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace WorkspaceSuggestions.Usage;

/// <summary>Counts what a run costs by counting the calls and reading what they report.</summary>
/// <remarks>
/// <para>
/// It records into whatever run is in flight and does nothing at all when none is, so the long-lived client
/// can be wrapped once and shared. Nothing about the call changes: the messages, the options, the response
/// and the stream are passed through untouched.
/// </para>
/// <para>
/// The two paths report usage differently, which is not obvious and is why both are handled.
/// <see cref="ChatResponse.Usage"/> carries it for a non-streamed call, but a streamed update has no
/// <c>Usage</c> property at all — the figure arrives as a <see cref="UsageContent"/> inside the update's
/// contents. A client that emits neither still has its call counted, so the call count is always a
/// measurement even when the tokenService counts cannot be.
/// </para>
/// </remarks>
internal sealed class UsageRecordingChatClient(IChatClient inner) : DelegatingChatClient(inner)
{
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        RunUsageScope.Current?.Called();

        var response = await base.GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);

        RunUsageScope.Current?.Record(response.Usage);

        return response;
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        RunUsageScope.Current?.Called();

        await foreach (var update in base
            .GetStreamingResponseAsync(messages, options, cancellationToken)
            .ConfigureAwait(false))
        {
            foreach (var content in update.Contents)
            {
                if (content is UsageContent usage)
                {
                    RunUsageScope.Current?.Record(usage.Details);
                }
            }

            yield return update;
        }
    }
}
