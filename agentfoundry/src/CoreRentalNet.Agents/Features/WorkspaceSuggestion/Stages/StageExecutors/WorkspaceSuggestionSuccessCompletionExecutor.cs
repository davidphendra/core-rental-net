using Microsoft.Agents.AI.Workflows;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Routing;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.StreamEvent;
using CoreRentalNet.Agents.Shared.Mcp;
using CoreRentalNet.Agents.Shared.ChatClients;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.StageExecutors;

/// <summary>Streams the approved setups, one event each, and then ends the run.</summary>
/// <remarks>
/// The setups arrive here already reviewed, so publishing them is a copy and not a decision. One event per setup
/// lets a caller render the first before the last has crossed the wire, and the ending is a separate event so a
/// setup's arrival never implies a successful run.
/// </remarks>
internal sealed class WorkspaceSuggestionSuccessCompletionExecutor(
    ITelemetryChatClient modelCallTelemetryChatClient,
    IMcpAccessTokenService accessTokenService)
    : WorkspaceSuggestionStreamingStageExecutor(WorkspaceWorkflowExecutorNames.SuccessCompletion)
{
    public override async ValueTask<WorkspaceSuggestionWorkflowState> HandleAsync(
        WorkspaceSuggestionWorkflowState workspaceSuggestionWorkflowState,
        IWorkflowContext workflowContext,
        CancellationToken cancellationToken = default)
    {
        // The run is over, so the credential it borrowed is given up here — before the ending is announced, and
        // long before the call's scope is disposed. A later stage that reached for it would find nothing, which is
        // the point: nothing after an ending is entitled to the caller's authority.
        accessTokenService.Release();

        workspaceSuggestionWorkflowState.RunStatus = WorkspaceSuggestionRunStatus.Success;

        foreach (var approvedWorkspaceSetup in workspaceSuggestionWorkflowState.ProposedWorkspaceSetups)
        {
            await PublishStreamEventAsync(
                workflowContext,
                WorkspaceSuggestionStreamEventMapper.CandidateApproved(
                    workspaceSuggestionWorkflowState, approvedWorkspaceSetup),
                cancellationToken);
        }

        await PublishStreamEventAsync(
            workflowContext,
            WorkspaceSuggestionStreamEventMapper.RunCompleted(workspaceSuggestionWorkflowState, modelCallTelemetryChatClient.Total),
            cancellationToken);

        return workspaceSuggestionWorkflowState;
    }
}
