namespace CoreRentalNet.Evaluation.Reporting;

/// <summary>One row that passed in the baseline and failed in the current run.</summary>
public sealed record CaseRegression(
    int RowNumber,
    string CaseId,
    string BaselineStability,
    string CurrentStability,
    IReadOnlyList<string> FailureCodes);
