namespace CoreRentalNet.CatalogIngestion.Reporting;

/// <summary>Where a step sends its progress, so the pipeline never names the display.</summary>
/// <remarks>
/// The pipeline depends on this one method and on nothing in Spectre.Console, which keeps the rendering a
/// display concern and lets a test record the reports with a hand-written fake.
/// </remarks>
internal interface IProgressReporter
{
    /// <summary>Publishes the current state of the step named in the update.</summary>
    void Report(ProgressUpdate update);

    /// <summary>Publishes the run's closing line, once the live display has been taken down.</summary>
    void Complete(string summary);
}
