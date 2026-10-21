using Microsoft.Extensions.AI;

namespace CoreRentalNet.Evaluation.Execution;

/// <summary>A catalogue that offers no tools, used when the evaluation run has no MCP endpoint.</summary>
public sealed class EmptyToolCatalog : IToolCatalog
{
    public Task<IReadOnlyList<AITool>> ListAsync(
        string callerToken,
        CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<AITool>>([]);
}
