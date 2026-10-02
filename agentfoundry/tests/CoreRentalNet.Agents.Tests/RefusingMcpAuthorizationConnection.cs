using Microsoft.Extensions.AI;
using CoreRentalNet.Agents.Shared.Mcp;

namespace CoreRentalNet.Agents.Tests;

/// <summary>A catalogue the caller's token cannot reach: it refuses every attempt to list its tools.</summary>
/// <remarks>
/// Written by hand rather than substituted, because the one thing it must prove is that a refusal ends the run
/// Unavailable and not with a stack trace, and the exception it throws is the seam that carries that decision.
/// </remarks>
internal sealed class RefusingMcpAuthorizationConnection : IMcpAuthorizationConnection
{
    /// <summary>It says it is configured, or the client would never ask it and the refusal would be untested.</summary>
    public bool IsConfigured => true;

    public Task<IReadOnlyList<AITool>> ToolsAsync(CancellationToken cancellationToken)
        => throw new CatalogueUnavailableException(
            "https://catalogue.test/mcp",
            new HttpRequestException("HTTP 401 Unauthorized"));
}
