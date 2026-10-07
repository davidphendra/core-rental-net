using Microsoft.Extensions.AI;
using CoreRentalNet.Agents.Shared.Mcp;

namespace CoreRentalNet.Agents.Tests;

/// <summary>A catalogue connection that records whether the caller's token was still held when it was closed.</summary>
/// <remarks>
/// Written by hand rather than substituted, because the one thing it must prove is an order — the session closes
/// before the token is released — and that is a fact about the token at close time, not about a network.
/// </remarks>
internal sealed class RecordingMcpAuthorizationConnection(IMcpAccessTokenService tokens) : IMcpAuthorizationConnection
{
    /// <summary>Whether the caller's token was still held when this connection was closed.</summary>
    public bool TokenWasPresentWhenClosed { get; private set; }

    public bool IsConfigured => true;

    public Task<IReadOnlyList<AITool>> ToolsAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<AITool>>([]);

    public ValueTask CloseAsync()
    {
        TokenWasPresentWhenClosed = !string.IsNullOrEmpty(tokens.Token);

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => CloseAsync();
}
