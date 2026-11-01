using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Loading;
using CoreRentalNet.Evaluation.Datasets.Validation;

using CoreRentalNet.Evaluation.Metrics.Retrieval;
using CoreRentalNet.Evaluation.Metrics.Statistics;
using CoreRentalNet.Evaluation.Metrics.Rules;
using CoreRentalNet.Evaluation.Metrics.Thresholds;
namespace CoreRentalNet.Evaluation.Metrics.Outcome;

/// <summary>One case's expected outcome beside the outcome the run actually produced.</summary>
public sealed record OutcomeRecord(
    string CaseId,
    string Category,
    RagOutcome Expected,
    RagOutcome Actual)
{
    /// <summary>Whether the run stated an outcome at all. An indeterminate run is not a wrong answer.</summary>
    public bool IsDeterminate => Actual != RagOutcome.Indeterminate;

    public bool IsCorrect => IsDeterminate && Actual == Expected;
}
