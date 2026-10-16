namespace CoreRentalNet.CatalogPopulate.Reporting;

/// <summary>One observation of the step that is running, sent while it runs.</summary>
/// <remarks>
/// <see cref="Completed"/> is the work done and <see cref="Total"/> the work expected, so a step reports a
/// fraction. <see cref="Detail"/> names what is being worked on right now, which is what tells a waiting
/// operator the tool is moving rather than hung.
/// </remarks>
internal sealed record ProgressUpdate(string Step, long Completed, long Total, string Detail = "");
