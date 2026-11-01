using Microsoft.Agents.AI;

using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;

namespace CoreRentalNet.Evaluation.Evaluation.AgentEval;

/// <summary>Gives a judge the projected item without changing the item the deterministic checks read.</summary>
public sealed class ProjectingEvaluator(
    IAgentEvaluator inner,
    AgentEvalRagContextProjector projector) : IAgentEvaluator
{
    public string Name => inner.Name;

    public Task<AgentEvaluationResults> EvaluateAsync(
        IReadOnlyList<EvalItem> items,
        string name,
        CancellationToken cancellationToken)
    {
        var projected = items.Select(projector.Project).ToArray();

        return inner.EvaluateAsync(projected, name, cancellationToken);
    }
}
