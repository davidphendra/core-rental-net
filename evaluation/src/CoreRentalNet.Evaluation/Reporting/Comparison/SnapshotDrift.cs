using CoreRentalNet.Evaluation.Reporting.Html;
using CoreRentalNet.Evaluation.Reporting.Writers;
using CoreRentalNet.Evaluation.Reporting.AgentEval;

namespace CoreRentalNet.Evaluation.Reporting.Comparison;

/// <summary>One case whose final setup differs from the baseline snapshot.</summary>
public sealed record SnapshotDrift(
    int RowNumber,
    string CaseId,
    IReadOnlyList<string> Differences);
