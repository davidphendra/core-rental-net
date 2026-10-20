using CoreRentalNet.Evaluation.Evidence;

namespace CoreRentalNet.Evaluation.Execution;

/// <summary>Reads the raw trace for one run.</summary>
public interface ITraceReader
{
    Task<RunTrace> ReadAsync(string runId, CancellationToken cancellationToken);
}
