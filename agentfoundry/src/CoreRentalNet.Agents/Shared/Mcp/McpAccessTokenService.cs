using Microsoft.Extensions.Logging;

namespace CoreRentalNet.Agents.Shared.Mcp;

/// <summary>The caller's own token, held for one call and given up when the call no longer needs it.</summary>
/// <remarks>
/// <b>Nothing here is cached and nothing is renewed.</b> The token is the caller's own, so its lifetime is the
/// identity provider's to decide and this class only decides how long it is <i>held</i>: from the request that
/// carried it to the end of the run it belongs to. Only the presence is ever logged, never the value.
/// </remarks>
internal sealed class McpAccessTokenService(ILogger<McpAccessTokenService> logger) : IMcpAccessTokenService
{
    /// <inheritdoc />
    public string? Token { get; set; }

    /// <inheritdoc />
    public ValueTask<string> GetAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return string.IsNullOrWhiteSpace(Token)
            ? throw new InvalidOperationException("The request carried no MCP access token.")
            : ValueTask.FromResult(Token);
    }

    /// <inheritdoc />
    public void Release()
    {
        // A second release is not an event: the ending that released it may be reached once, but a caller asking
        // twice should not turn "already given up" into a log line that reads like a leak.
        if (Token is null)
        {
            return;
        }

        Token = null;

        logger.LogInformation(
            "The caller's catalogue token was released at the end of the run; nothing will present it again.");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // The backstop, and silent on purpose: the release above is the event worth reading, and this is what
        // makes the holder's lifetime the call's even on a path that never reaches an ending.
        Token = null;
    }
}
