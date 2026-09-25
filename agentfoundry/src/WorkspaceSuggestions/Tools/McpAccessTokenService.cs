namespace WorkspaceSuggestions.Tools;

/// <summary>The caller's own token, held for one call.</summary>
/// <remarks>
/// Replaces the client-credentials exchange: the credential is the caller's, so nothing is fetched and no secret
/// lives in the container. A call that reaches the catalogue without one fails loudly rather than presenting
/// the process as the caller.
/// </remarks>
internal sealed class McpAccessTokenService : IMcpAccessTokenService
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
}
