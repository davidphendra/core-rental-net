using CoreRentalNet.Evaluation.Cases.Model;
using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;
using CoreRentalNet.Evaluation.Execution.Transport;
using CoreRentalNet.Evaluation.Execution.Auth;
using CoreRentalNet.Evaluation.Execution.Tools;
using CoreRentalNet.Evaluation.Execution.Running;

using CoreRentalNet.Evaluation.Runs.Record;
using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Assembly;
namespace CoreRentalNet.Evaluation.Runs.Result;

/// <summary>Runs an ad-hoc query through the same executor the datasets use.</summary>
/// <remarks>
/// This is a façade, not a second execution path: it wraps the query in a one-case set so the per-case budget,
/// the trace read and the concurrency gate apply exactly as they do to a dataset run.
/// </remarks>
public sealed class QueryRagRunner(EvaluationRunner runner, RowRunRagProjector projector) : IRagEvaluationRunner
{
    public async Task<RagRunResult> RunAsync(string query, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        var caseSet = new EvaluationCaseSet(
            [new EvaluationCase(1, "adhoc-1", query, [new ExpectedSetupVariant([])], null, null)]);

        var runs = await runner.RunAsync(caseSet, repetitions: 1, cancellationToken).ConfigureAwait(false);
        return projector.Project(runs[0]);
    }
}
