using CoreRentalNet.Evaluation.Cases;
using CoreRentalNet.Evaluation.Execution;
using Microsoft.Agents.AI;

namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>Runs the built-in evaluators over one set of trace-enriched items.</summary>
public sealed class EvaluationPipeline(
    EvalItemFactory itemFactory,
    IReadOnlyList<IAgentEvaluator> evaluators,
    EvaluationReportFactory reportFactory)
{
    public async Task<EvaluationReport> EvaluateAsync(
        IReadOnlyList<RowRun> runs,
        EvaluationScope scope,
        int totalRows,
        string? stopReason,
        CancellationToken cancellationToken)
    {
        var items = itemFactory.Create(runs);
        var results = new List<AgentEvaluationResults>(evaluators.Count);

        foreach (var evaluator in evaluators)
        {
            cancellationToken.ThrowIfCancellationRequested();

            results.Add(await evaluator.EvaluateAsync(
                items,
                evaluator.Name,
                cancellationToken));
        }

        return reportFactory.Create(runs, items, results, scope, totalRows, stopReason);
    }
}
