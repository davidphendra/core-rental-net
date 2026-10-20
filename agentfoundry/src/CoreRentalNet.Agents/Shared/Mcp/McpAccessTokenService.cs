using Microsoft.Extensions.Logging;

namespace CoreRentalNet.Agents.Shared.Mcp;

/// <summary>Compatibility seam for tests written before the token moved to the run context.</summary>
internal sealed class McpAccessTokenService(ILogger<McpAccessTokenService> logger) : IMcpAccessTokenService
{
    public string? Token { get; set; }

    public ValueTask<string> GetAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return string.IsNullOrWhiteSpace(Token)
            ? throw new InvalidOperationException("The request carried no MCP access token.")
            : ValueTask.FromResult(Token);
    }

    public void Release()
    {
        Token = null;
        logger.LogInformation("The caller's catalogue token was released at the end of the run.");
    }

    public void Dispose() => Token = null;
}
