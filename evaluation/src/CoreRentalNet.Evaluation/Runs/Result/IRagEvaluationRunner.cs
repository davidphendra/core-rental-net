using CoreRentalNet.Evaluation.Runs.Record;
using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Assembly;

namespace CoreRentalNet.Evaluation.Runs.Result;

/// <summary>Executes one query against the agent under test and returns the stage-separated result.</summary>
public interface IRagEvaluationRunner
{
    Task<RagRunResult> RunAsync(string query, CancellationToken cancellationToken = default);
}
