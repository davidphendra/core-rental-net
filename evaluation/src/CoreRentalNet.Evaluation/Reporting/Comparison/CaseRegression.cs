using CoreRentalNet.Evaluation.Reporting.Html;
using CoreRentalNet.Evaluation.Reporting.Writers;
using CoreRentalNet.Evaluation.Reporting.AgentEval;

namespace CoreRentalNet.Evaluation.Reporting.Comparison;

/// <summary>One row that passed in the baseline and failed in the current run.</summary>
public sealed record CaseRegression(
    int RowNumber,
    string CaseId,
    string BaselineStability,
    string CurrentStability,
    IReadOnlyList<string> FailureCodes);
