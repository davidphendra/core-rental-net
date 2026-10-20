using Microsoft.Extensions.AI;

namespace CoreRentalNet.Evaluation.Execution;

/// <summary>The catalogue tools the agent was offered, as real AIFunction definitions.</summary>
public interface IToolCatalog
{
    Task<IReadOnlyList<AITool>> ListAsync(CancellationToken cancellationToken);
}
