using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Loading;
using CoreRentalNet.Evaluation.Datasets.Validation;

using CoreRentalNet.Evaluation.Runs.Record;
using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Assembly;
namespace CoreRentalNet.Evaluation.Runs.Result;

/// <summary>Maps the status the agent reports onto the three outcomes the evaluation grades.</summary>
/// <remarks>
/// An unrecognised or absent status becomes <see cref="RagOutcome.Indeterminate"/> rather than a guess. Coercing
/// a missing status into <c>rejected</c> would count a crash as a correct refusal, which is the one error a
/// rejection-heavy test set would reward.
/// </remarks>
public static class OutcomeExtractor
{
    private const string MissingStatus = "missing";

    public static RagOutcome From(string? runStatus) => runStatus?.Trim().ToLowerInvariant() switch
    {
        "success" => RagOutcome.Success,
        "rejected" => RagOutcome.Rejected,
        "unavailable" => RagOutcome.Unavailable,
        _ => RagOutcome.Indeterminate,
    };

    /// <summary>Whether the run stated a status the vocabulary recognises at all.</summary>
    public static bool IsRecognised(string? runStatus)
        => From(runStatus) != RagOutcome.Indeterminate;

    /// <summary>The status a run that never completed reports.</summary>
    public static string Missing => MissingStatus;
}
