using CoreRentalNet.Evaluation.Runs.Record;
using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Result;
using CoreRentalNet.Evaluation.Runs.Assembly;

using CoreRentalNet.Evaluation.Analysis.Category;
namespace CoreRentalNet.Evaluation.Analysis.Generalization;

/// <summary>The development-versus-test difference for each metric both runs measured.</summary>
/// <remarks>
/// A positive gap is not evidence of overfitting on its own: development data shaped the implementation, so its
/// score is optimistic by construction. The report states that beside every number rather than letting a reader
/// infer a cause from a subtraction.
/// </remarks>
public sealed record GeneralizationGapReport(
    IReadOnlyList<GeneralizationGap> Gaps,
    string Caveat)
{
    public static string StandardCaveat =>
        "Development scores are optimistic: the development set informed the implementation, so the gap is a "
        + "diagnostic, not an unbiased estimate of overfitting.";
}

/// <summary>One metric's development score, test score and their difference.</summary>
public sealed record GeneralizationGap(string Metric, double? Development, double? Test)
{
    public double? Gap => Development is null || Test is null ? null : Development - Test;
}
