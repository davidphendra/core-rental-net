using Microsoft.Extensions.AI;

using CoreRentalNet.Evaluation.Execution.Transport;
using CoreRentalNet.Evaluation.Execution.Auth;
using CoreRentalNet.Evaluation.Execution.Running;
namespace CoreRentalNet.Evaluation.Execution.Tools;

/// <summary>A catalogue that offers no tools, used when the evaluation run has no MCP endpoint.</summary>
public sealed class EmptyToolCatalog : IToolCatalog
{
    public Task<IReadOnlyList<AITool>> ListAsync(
        string callerToken,
        CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<AITool>>([]);
}
