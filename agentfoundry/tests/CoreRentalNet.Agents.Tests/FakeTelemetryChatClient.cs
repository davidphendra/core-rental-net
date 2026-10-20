using CoreRentalNet.Agents.Shared.ChatClients;
using CoreRentalNet.Agents.Shared.Model;
using Microsoft.Extensions.AI;

namespace CoreRentalNet.Agents.Tests;

/// <summary>A telemetry client with no model behind it, for observing the decorator.</summary>
internal sealed class FakeTelemetryChatClient : ITelemetryChatClient
{
    public AgentRunUsage Total { get; } = new(0, 0, 0, "gpt-test", "prompt-test");

    public List<string> Lifecycle { get; } = [];

    public void StartRun(string runId) => Lifecycle.Add($"start:{runId}");

    public void CompleteRun(string runStatus) => Lifecycle.Add($"complete:{runStatus}");

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "answer")));

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
