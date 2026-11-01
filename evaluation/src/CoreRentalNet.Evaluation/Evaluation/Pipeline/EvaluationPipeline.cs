using CoreRentalNet.Evaluation.Cases.Model;
using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;
using CoreRentalNet.Evaluation.Execution.Transport;
using CoreRentalNet.Evaluation.Execution.Auth;
using CoreRentalNet.Evaluation.Execution.Tools;
using CoreRentalNet.Evaluation.Execution.Running;
using Microsoft.Agents.AI;

using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;
namespace CoreRentalNet.Evaluation.Evaluation.Pipeline;

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
