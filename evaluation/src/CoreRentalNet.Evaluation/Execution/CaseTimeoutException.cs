namespace CoreRentalNet.Evaluation.Execution;

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
