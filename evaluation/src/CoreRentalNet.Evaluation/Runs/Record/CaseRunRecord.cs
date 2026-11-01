using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Loading;
using CoreRentalNet.Evaluation.Datasets.Validation;

using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Result;
using CoreRentalNet.Evaluation.Runs.Assembly;
namespace CoreRentalNet.Evaluation.Runs.Record;

/// <summary>One case as stored in a run record.</summary>
public sealed record CaseRunRecord(
    string CaseId,
    string Category,
    RagOutcome Expected,
    RagOutcome Actual,
    bool Correct,
    IReadOnlyList<string> RecommendedSkus,
    IReadOnlyList<string> FailureCodes)
{
    /// <summary>The user request, so a failure can be read without opening the dataset.</summary>
    public string Query { get; init; } = string.Empty;

    /// <summary>What retrieval returned, which is often where the failure started.</summary>
    public IReadOnlyList<string> RetrievedSkus { get; init; } = [];

    /// <summary>The first metric the case broke, or null when it broke none.</summary>
    public string? FailedMetric { get; init; }
}
