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
        CancellationToken cancellationToken)
    {
        var items = await itemFactory.CreateAsync(runs, cancellationToken);
        var results = new List<AgentEvaluationResults>(evaluators.Count);

        foreach (var evaluator in evaluators)
        {
            cancellationToken.ThrowIfCancellationRequested();

            results.Add(await evaluator.EvaluateAsync(
                items,
                evaluator.Name,
                cancellationToken));
        }

        return reportFactory.Create(runs, items, results);
    }
}
