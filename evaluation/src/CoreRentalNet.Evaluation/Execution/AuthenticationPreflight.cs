using CoreRentalNet.Evaluation.Evidence;
using Microsoft.Extensions.AI;

namespace CoreRentalNet.Evaluation.Execution;

/// <summary>Checks the token and the catalogue tools before the first case runs.</summary>
public sealed class AuthenticationPreflight(
    IToolCatalog toolCatalog,
    TimeProvider timeProvider)
{
    /// <summary>The tools a successful preflight discovered.</summary>
    public async Task<IReadOnlyList<AITool>> RunAsync(
        CallerToken token,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(token);

        var now = timeProvider.GetUtcNow();

        if (token.ExpiresAt is null)
        {
            throw new AuthenticationPreflightException("The caller token has no expiry claim.");
        }

        if (token.ExpiresAt <= now)
        {
            throw new AuthenticationPreflightException(
                $"The caller token expired at {token.ExpiresAt:O}.");
        }

        IReadOnlyList<AITool> tools;

        try
        {
            tools = await toolCatalog.ListAsync(token.Value, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new AuthenticationPreflightException(
                $"The MCP tool catalogue refused the caller token: {exception.Message}",
                exception);
        }

        var names = tools.Select(tool => tool.Name).ToHashSet(StringComparer.Ordinal);
        var missing = new[] { WorkspaceToolNames.NameSearch, WorkspaceToolNames.MeaningSearch }
            .Where(name => !names.Contains(name))
            .ToArray();

        if (missing.Length > 0)
        {
            throw new AuthenticationPreflightException(
                $"The MCP tool catalogue is missing {string.Join(", ", missing)}.");
        }

        return tools;
    }
}
