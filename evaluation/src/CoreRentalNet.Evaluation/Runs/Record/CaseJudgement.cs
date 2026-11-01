using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Result;
using CoreRentalNet.Evaluation.Runs.Assembly;

namespace CoreRentalNet.Evaluation.Runs.Record;

/// <summary>One judge decision, kept with the reason it was made so a human can review it.</summary>
/// <remarks>
/// A judge score without its explanation is unauditable. The model is stored beside it because a score from one
/// model cannot be compared with a score from another.
/// </remarks>
public sealed record CaseJudgement(
    string CaseId,
    string Metric,
    double? Value,
    string Explanation,
    string Model);
