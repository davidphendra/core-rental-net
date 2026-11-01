using Microsoft.Extensions.AI;

using CoreRentalNet.Evaluation.Execution.Transport;
using CoreRentalNet.Evaluation.Execution.Auth;
using CoreRentalNet.Evaluation.Execution.Running;
namespace CoreRentalNet.Evaluation.Execution.Tools;

/// <summary>The catalogue tools the agent was offered, as real AIFunction definitions.</summary>
public interface IToolCatalog
{
    Task<IReadOnlyList<AITool>> ListAsync(
        string callerToken,
        CancellationToken cancellationToken);
}
