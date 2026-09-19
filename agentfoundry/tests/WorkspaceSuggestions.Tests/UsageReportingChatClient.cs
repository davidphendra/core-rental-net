using Microsoft.Extensions.AI;

namespace WorkspaceSuggestions.Tests;

/// <summary>A chat client that reports what it cost, on both the streamed and the unstreamed path.</summary>
/// <remarks>
/// Hand-written, as this repository does everywhere else. It is deliberately able to report
/// <b>nothing</b> as well as something, because "a client that does not say what it cost" is a real case the
/// recorder has to survive rather than a hypothetical one.
/// </remarks>
internal sealed class UsageReportingChatClient(UsageDetails? usage) : IChatClient
{
    public int Calls { get; private set; }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Calls++;

        await Task.Yield();

        var update = new ChatResponseUpdate(ChatRole.Assistant, "{\"status\":\"spec\"}");

        if (usage is not null)
        {
            update.Contents.Add(new UsageContent(usage));
        }

        yield return update;
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        Calls++;

        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "{}")) { Usage = usage });
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
