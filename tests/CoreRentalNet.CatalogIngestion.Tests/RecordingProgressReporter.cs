using CoreRentalNet.CatalogIngestion.Reporting;

namespace CoreRentalNet.CatalogIngestion.Tests;

/// <summary>Records every progress update, so a test can assert the reported steps without a live display.</summary>
internal sealed class RecordingProgressReporter : IProgressReporter
{
    /// <summary>The updates in the order they were reported.</summary>
    public List<ProgressUpdate> Updates { get; } = [];

    /// <summary>The closing lines, in the order they were reported.</summary>
    public List<string> Summaries { get; } = [];

    public void Report(ProgressUpdate update) => Updates.Add(update);

    public void Complete(string summary) => Summaries.Add(summary);

    /// <summary>The distinct steps, in the order they first reported.</summary>
    public string[] Steps()
        => Updates.Select(update => update.Step).Distinct(StringComparer.Ordinal).ToArray();

    /// <summary>The last update for a step, which is where its total is known to be complete.</summary>
    public ProgressUpdate Last(string step)
        => Updates.Last(update => string.Equals(update.Step, step, StringComparison.Ordinal));
}
