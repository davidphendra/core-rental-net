using Microsoft.Agents.AI.Workflows;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Routing;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.StreamEventPublishing;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.StageExecutors;

/// <summary>Counts the attempt, decides what happens next, and tells the caller a retry is beginning.</summary>
/// <remarks>
/// Whether a run tries again is the workflow's business and not a model's, so the decision lives here rather
/// than on the reviewer. The attempt count is the reviewer's, because a review is what finishes an attempt;
/// this reads it, so the third review is the one that ends an unavailable run.
/// </remarks>
internal sealed class WorkspaceSetupRetryDecisionExecutor(WorkspaceSetupRetryDecisionPolicy retryDecisionPolicy)
    : WorkspaceSuggestionStreamingStageExecutor(WorkspaceWorkflowExecutorNames.RetryDecision)
{
    public override async ValueTask<WorkspaceSuggestionWorkflowState> HandleAsync(
        WorkspaceSuggestionWorkflowState workspaceSuggestionWorkflowState,
        IWorkflowContext workflowContext,
        CancellationToken cancellationToken = default)
    {
        var reviewResult = workspaceSuggestionWorkflowState.WorkspaceSetupReview
            ?? new WorkspaceSetupReviewResult(IsAcceptable: false, [], "The setup set was not reviewed.");

        var retryDecision = retryDecisionPolicy.DecideRetryAfterReview(
            workspaceSuggestionWorkflowState.CompletedAttemptCount, reviewResult);

        workspaceSuggestionWorkflowState.RetryDecision = retryDecision;

        if (retryDecision is WorkspaceSetupRetryDecision.RetryWithRephrasing)
        {
            workspaceSuggestionWorkflowState.PreviousAttemptIssues = reviewResult.Issues;

            await PublishStreamEventAsync(
                workflowContext,
                WorkspaceSuggestionStreamEventMapper.RetryStarted(workspaceSuggestionWorkflowState),
                cancellationToken);
        }

        return workspaceSuggestionWorkflowState;
    }
}
