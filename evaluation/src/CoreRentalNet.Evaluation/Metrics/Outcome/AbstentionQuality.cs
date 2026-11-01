using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Loading;
using CoreRentalNet.Evaluation.Datasets.Validation;

using CoreRentalNet.Evaluation.Metrics.Retrieval;
using CoreRentalNet.Evaluation.Metrics.Statistics;
using CoreRentalNet.Evaluation.Metrics.Rules;
using CoreRentalNet.Evaluation.Metrics.Thresholds;
namespace CoreRentalNet.Evaluation.Metrics.Outcome;

/// <summary>Whether the application abstains when it should, and answers when it should.</summary>
/// <remarks>
/// False acceptance is a recommendation offered for a request the rules forbid or the catalogue cannot serve.
/// False rejection is a refusal of a request that was answerable. They are separate rates because they fail
/// differently: one rents the wrong workspace, the other turns a customer away.
/// </remarks>
public static class AbstentionQuality
{
    public static AbstentionReport Evaluate(IReadOnlyList<OutcomeRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        var determinate = records.Where(record => record.IsDeterminate).ToArray();
        var abstainable = determinate.Where(record => record.Expected != RagOutcome.Success).ToArray();
        var answerable = determinate.Where(record => record.Expected == RagOutcome.Success).ToArray();

        var falseAccept = abstainable.Count(record => record.Actual == RagOutcome.Success);
        var falseReject = answerable.Count(record => record.Actual != RagOutcome.Success);

        return new AbstentionReport(
            Cases: records.Count,
            Indeterminate: records.Count - determinate.Length,
            Abstainable: abstainable.Length,
            CorrectAbstentions: abstainable.Length - falseAccept,
            FalseAcceptanceRate: Rate(falseAccept, abstainable.Length),
            Answerable: answerable.Length,
            CorrectAnswers: answerable.Length - falseReject,
            FalseRejectionRate: Rate(falseReject, answerable.Length));
    }

    private static double? Rate(int count, int total) => total == 0 ? null : count / (double)total;
}

/// <summary>The abstention rates a run reports, with the sample each was measured over.</summary>
public sealed record AbstentionReport(
    int Cases,
    int Indeterminate,
    int Abstainable,
    int CorrectAbstentions,
    double? FalseAcceptanceRate,
    int Answerable,
    int CorrectAnswers,
    double? FalseRejectionRate);
