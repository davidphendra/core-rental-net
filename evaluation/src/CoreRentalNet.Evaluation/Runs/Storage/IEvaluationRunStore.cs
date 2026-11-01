using CoreRentalNet.Evaluation.Runs.Record;
using CoreRentalNet.Evaluation.Runs.Result;
using CoreRentalNet.Evaluation.Runs.Assembly;

namespace CoreRentalNet.Evaluation.Runs.Storage;

/// <summary>Persists one run so a later run can be compared with it.</summary>
public interface IEvaluationRunStore
{
    Task<string> SaveAsync(EvaluationRunRecord record, CancellationToken cancellationToken = default);
}
