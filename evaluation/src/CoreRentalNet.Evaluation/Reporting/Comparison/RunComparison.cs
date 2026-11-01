using CoreRentalNet.Evaluation.Runs.Record;
using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Result;
using CoreRentalNet.Evaluation.Runs.Assembly;

using CoreRentalNet.Evaluation.Reporting.Html;
using CoreRentalNet.Evaluation.Reporting.Writers;
using CoreRentalNet.Evaluation.Reporting.AgentEval;
namespace CoreRentalNet.Evaluation.Reporting.Comparison;

/// <summary>Whether two runs may be compared at all, and what changed between them.</summary>
/// <remarks>
/// A metric that moved is only meaningful when the dataset hash and the judge are unchanged. The comparability
/// warnings are produced first, so a reader sees them before the deltas rather than after.
/// </remarks>
public sealed record RunComparisonReport(
    IReadOnlyList<string> Incompatibilities,
    IReadOnlyList<MetricDelta> Metrics);

/// <summary>One metric on two runs.</summary>
public sealed record MetricDelta(string Metric, double? Baseline, double? Current, double? Delta);
