using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;

/// <summary>Decides what the workflow does after one attempt's review, and never asks a model to.</summary>
/// <remarks>
/// The reviewer answers whether a setup set is acceptable; whether another attempt happens is the workflow's,
/// and it is deterministic: an acceptable review ends the run, an attempt at the limit ends it unavailable, and
/// anything else reads the sentence again. A retry never consumes the transport's own retry budget, which is a
/// different mechanism entirely.
/// </remarks>
public sealed class WorkspaceSetupRetryDecisionPolicy(int maximumAttemptCount = 3)
{
    public int MaximumAttemptCount { get; } =
        maximumAttemptCount > 0 ? maximumAttemptCount : throw new ArgumentOutOfRangeException(
            nameof(maximumAttemptCount), maximumAttemptCount, "The attempt limit must be greater than zero.");

    /// <summary>What happens next, from the verdict and how many attempts have finished.</summary>
    public WorkspaceSetupRetryDecision DecideRetryAfterReview(
        int completedAttemptCount,
        WorkspaceSetupReviewResult workspaceSetupReviewResult)
    {
        ArgumentNullException.ThrowIfNull(workspaceSetupReviewResult);

        return workspaceSetupReviewResult.IsAcceptable
            ? WorkspaceSetupRetryDecision.Accepted
            : completedAttemptCount >= MaximumAttemptCount
                ? WorkspaceSetupRetryDecision.AttemptsExhausted
                : WorkspaceSetupRetryDecision.RetryWithRephrasing;
    }
}
