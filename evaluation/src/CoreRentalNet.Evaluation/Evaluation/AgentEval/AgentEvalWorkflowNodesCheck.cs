using AgentEval.Assertions;
using CoreRentalNet.Evaluation.Evidence;
using Microsoft.Agents.AI;

namespace CoreRentalNet.Evaluation.Evaluation.AgentEval;

/// <summary>The AgentEval workflow-node assertions, expressed as a built-in evaluation check.</summary>
public static class AgentEvalWorkflowNodesCheck
{
    public static EvalCheck Create()
        => FunctionEvaluator.Create(
            MetricNames.AgentEvalWorkflowNodes,
            (EvalItem item) => Evaluate(item));

    private static EvalCheckResult Evaluate(EvalItem item)
    {
        var context = EvaluationContext.Parse(item.Context);
        var workflow = AgentEvalWorkflowProjector.Project(context, item.Query);

        var assertions = workflow.Should()
            .HaveGraphStructure()
            .HaveNodes([.. AgentEvalWorkflowProjector.DeclaredNodes])
            .HaveNoErrors();

        foreach (var node in AgentEvalWorkflowProjector.DeclaredNodes)
        {
            assertions.HaveInvokedExecutor(node);
        }

        return new EvalCheckResult(
            assertions.IsValid,
            assertions.IsValid
                ? "AgentEval workflow-node assertions passed."
                : string.Join("; ", assertions.Failures),
            MetricNames.AgentEvalWorkflowNodes);
    }
}
