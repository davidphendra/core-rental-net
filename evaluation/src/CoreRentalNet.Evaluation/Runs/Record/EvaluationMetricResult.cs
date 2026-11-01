using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Result;
using CoreRentalNet.Evaluation.Runs.Assembly;

namespace CoreRentalNet.Evaluation.Runs.Record;

/// <summary>One metric in a stored run: its score, its sample, its interval and whether it may be read as evidence.</summary>
/// <remarks>
/// The status is separate from the score on purpose. A metric with a sample too small to support an interval is
/// reported with its number and marked, rather than being rounded to a pass or a fail.
/// </remarks>
public sealed record EvaluationMetricResult(
    string Metric,
    double? Score,
    int SampleSize,
    int Excluded,
    double ConfidenceLevel,
    double? Lower,
    double? Upper,
    string Status)
{
    /// <summary>The configured pass line, when one exists for this metric.</summary>
    public double? Threshold { get; init; }

    /// <summary>The verdict against that line; null when no line is configured or the sample cannot support one.</summary>
    public bool? Passed { get; init; }

    public bool IsEvaluable => string.Equals(Status, "evaluable", StringComparison.Ordinal);
}
