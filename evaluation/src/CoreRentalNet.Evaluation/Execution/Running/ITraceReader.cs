using CoreRentalNet.Evaluation.Evidence.Trace;
using CoreRentalNet.Evaluation.Evidence.Projection;
using CoreRentalNet.Evaluation.Evidence.Context;

using CoreRentalNet.Evaluation.Execution.Transport;
using CoreRentalNet.Evaluation.Execution.Auth;
using CoreRentalNet.Evaluation.Execution.Tools;
namespace CoreRentalNet.Evaluation.Execution.Running;

/// <summary>Reads the raw trace for one run.</summary>
public interface ITraceReader
{
    Task<RunTrace> ReadAsync(string runId, CancellationToken cancellationToken);
}
