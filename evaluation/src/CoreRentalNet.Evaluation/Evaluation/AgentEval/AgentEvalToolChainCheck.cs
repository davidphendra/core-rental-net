using AgentEval.Assertions;
using AgentEval.Output;
using CoreRentalNet.Evaluation.Evidence;
using Microsoft.Agents.AI;

namespace CoreRentalNet.Evaluation.Evaluation.AgentEval;

/// <summary>The AgentEval tool-chain assertions, expressed as a built-in evaluation check.</summary>
public static class AgentEvalToolChainCheck
{
    public static EvalCheck Create()
        => FunctionEvaluator.Create(
            MetricNames.AgentEvalToolChain,
            (EvalItem item) => Evaluate(item));

    private static EvalCheckResult Evaluate(EvalItem item)
    {
        var context = EvaluationContext.Parse(item.Context);
        var report = AgentEvalToolReportProjector.Project(context);

        using var scope = AgentEvalScope.Collecting("tool-chain");

        if (report.Count == 0)
        {
            report.Should().HaveCalledTool(
                WorkspaceToolNames.NameSearch,
                because: "the agent must search the catalogue before it answers");
        }
        else
        {
            report.Should().HaveNoErrors(
                because: "a failed catalogue call is not a completed answer");

            if (report.WasToolCalled(WorkspaceToolNames.NameSearch)
                && report.WasToolCalled(WorkspaceToolNames.MeaningSearch))
            {
                report.Should().HaveCallOrder(
                    WorkspaceToolNames.NameSearch,
                    WorkspaceToolNames.MeaningSearch);
            }
        }

        scope.Dispose();

        var failures = scope.Results
            .Where(result => result.Outcome != AssertionOutcome.Passed)
            .ToArray();

        return new EvalCheckResult(
            failures.Length == 0,
            failures.Length == 0
                ? "AgentEval tool-chain assertions passed."
                : string.Join("; ", failures.Select(result => result.Message)),
            MetricNames.AgentEvalToolChain);
    }
}
