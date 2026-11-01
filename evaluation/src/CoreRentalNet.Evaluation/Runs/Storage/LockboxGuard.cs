using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Loading;
using CoreRentalNet.Evaluation.Datasets.Validation;

using CoreRentalNet.Evaluation.Runs.Record;
using CoreRentalNet.Evaluation.Runs.Result;
using CoreRentalNet.Evaluation.Runs.Assembly;
namespace CoreRentalNet.Evaluation.Runs.Storage;

/// <summary>Decides whether a run may proceed, given what has already been evaluated.</summary>
/// <remarks>
/// The lockbox rule is enforced before any case executes, not after. A guard that only labelled the result would
/// let a second, accidental evaluation of the same cases happen and then merely describe it as such.
/// </remarks>
public static class LockboxGuard
{
    public static IReadOnlyList<string> Violations(
        RagEvaluationDataset dataset,
        bool confirmed,
        bool reEvaluate,
        IReadOnlyList<string> previouslyEvaluatedHashes)
    {
        ArgumentNullException.ThrowIfNull(dataset);
        ArgumentNullException.ThrowIfNull(previouslyEvaluatedHashes);

        if (!dataset.IsLockbox)
        {
            return [];
        }

        if (!confirmed)
        {
            return [$"'{dataset.Name}' is the lockbox. Re-run with --confirm-lockbox to evaluate it."];
        }

        var alreadyRun = previouslyEvaluatedHashes.Contains(dataset.Sha256, StringComparer.Ordinal);
        return alreadyRun && !reEvaluate
            ? [$"This lockbox hash has already been evaluated. Pass --re-evaluate-lockbox only if you accept that the result is no longer independent."]
            : [];
    }
}
