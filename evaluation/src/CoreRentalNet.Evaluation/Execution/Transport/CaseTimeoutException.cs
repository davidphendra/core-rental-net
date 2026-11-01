using CoreRentalNet.Evaluation.Execution.Auth;
using CoreRentalNet.Evaluation.Execution.Tools;
using CoreRentalNet.Evaluation.Execution.Running;

namespace CoreRentalNet.Evaluation.Execution.Transport;

/// <summary>A case exceeded its execution budget, so the run stopped.</summary>
public sealed class CaseTimeoutException : Exception
{
    public CaseTimeoutException(IReadOnlyList<RowRun> runs, TimeSpan timeout)
        : base($"A case exceeded the {timeout.TotalSeconds:0}-second execution budget.")
    {
        Runs = runs;
    }

    /// <summary>The runs up to and including the timed-out case.</summary>
    public IReadOnlyList<RowRun> Runs { get; }
}
