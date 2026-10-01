using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace CoreRentalNet.Agents.Tests;

/// <summary>A chat client that reports usage on every reply, so a test can prove what a run counted.</summary>
/// <remarks>
/// Hand-written rather than mocked, as this repository does everywhere else. <see cref="FixedChatClient"/> reports
/// no usage, which is the shape a call whose transport failed has; this fake is the shape a completed call has, and
/// it is what makes the token totals observable.
/// </remarks>
internal sealed class UsageReportingChatClient(int inputTokens, int outputTokens) : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "a desk and a chair"))
        {
            Usage = new UsageDetails { InputTokenCount = inputTokens, OutputTokenCount = outputTokens },
        });

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        yield return new ChatResponseUpdate(ChatRole.Assistant, "a desk and a chair");

        // What the call cost arrives on the stream, as a provider reports it, and not on the text update.
        var usage = new ChatResponseUpdate();
        usage.Contents.Add(new UsageContent(
            new UsageDetails { InputTokenCount = inputTokens, OutputTokenCount = outputTokens }));

        yield return usage;
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
