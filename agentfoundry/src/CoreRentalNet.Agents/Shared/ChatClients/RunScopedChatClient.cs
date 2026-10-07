using Microsoft.Extensions.AI;
using CoreRentalNet.Agents.Shared.Scoping;

namespace CoreRentalNet.Agents.Shared.ChatClients;

/// <summary>An <see cref="IChatClient"/> that belongs to the graph, not the run: every call reaches the run's own.</summary>
/// <remarks>
/// <b>The graph is resolved once, from the root container, so a stage agent built at that point cannot hold the
/// run's telemetry.</b> This forwards each call to the request's own <see cref="ITelemetryChatClient"/>, so the
/// totals and the run's span are recorded per run and not shared between concurrent runs. The request's scope
/// owns and disposes that instance, which is why this one disposes nothing.
/// </remarks>
internal sealed class RunScopedChatClient(IRunScope runScope) : IChatClient
{
    /// <inheritdoc />
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => Current.GetResponseAsync(messages, options, cancellationToken);

    /// <inheritdoc />
    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => Current.GetStreamingResponseAsync(messages, options, cancellationToken);

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
        => runScope.TryResolve<ITelemetryChatClient>(out var client)
            ? client.GetService(serviceType, serviceKey)
            : null;

    /// <inheritdoc />
    public void Dispose()
    {
        // The run's client belongs to the request's scope; this graph-level forwarder owns nothing to dispose.
    }

    private ITelemetryChatClient Current => runScope.Resolve<ITelemetryChatClient>();
}
