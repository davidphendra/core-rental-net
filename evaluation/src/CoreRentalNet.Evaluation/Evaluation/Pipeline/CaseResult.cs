using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;

namespace CoreRentalNet.Evaluation.Evaluation.Pipeline;

/// <summary>One row's three repetitions, aggregated by majority.</summary>
public sealed record CaseResult(
    int RowNumber,
    string CaseId,
    string Sentence,
    IReadOnlyList<RepetitionResult> Repetitions,
    bool CasePass,
    string Stability,
    IReadOnlyList<string> FailureCodes,
    double MeanAnswerF1,
    double MeanRetrievalRecall)
{
    /// <summary>The final workspace setup the agent returned, as the judge and snapshots read it.</summary>
    public string ActualOutput { get; init; } = string.Empty;
}
