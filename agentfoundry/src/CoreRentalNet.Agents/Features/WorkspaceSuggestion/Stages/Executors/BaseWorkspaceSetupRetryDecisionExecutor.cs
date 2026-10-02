using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Logging;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Routing;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.StreamEvent;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Executors;

/// <summary>Counts the attempt, decides what happens next, and tells the caller a retry is beginning.</summary>
/// <remarks>
/// Whether a run tries again is the workflow's business and not a model's, so the decision lives here rather
/// than on the reviewer. The attempt count is the reviewer's, because a review is what finishes an attempt;
/// this reads it, so the third review is the one that ends an unavailable run.
/// </remarks>
internal sealed class BaseWorkspaceSetupRetryDecisionExecutor(
    WorkspaceSetupRetryDecisionPolicy retryDecisionPolicy,
    ILogger logger)
    : BaseWorkspaceSuggestionStreamingExecutor(WorkspaceWorkflowExecutorNames.RetryDecision, logger)
{
    protected override async ValueTask<WorkspaceSuggestionWorkflowState> ProcessAsync(
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
