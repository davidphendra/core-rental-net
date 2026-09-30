using Microsoft.Agents.AI.Workflows;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.StreamingEvents;

using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Routing;

using CoreRentalNet.Agents.Features.WorkspaceSuggestion.StreamEventPublishing;
using CoreRentalNet.Agents.Shared.Mcp;
using CoreRentalNet.Agents.Shared.Model;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.StageExecutors;

/// <summary>Ends a run whose sentence was not about a workspace at all, with no downstream stage run.</summary>
internal sealed class WorkspaceSuggestionRejectionCompletionExecutor(
    AgentRunUsageAccumulator runUsage,
    IMcpAccessTokenService accessTokens)
    : WorkspaceSuggestionStreamingStageExecutor(WorkspaceWorkflowExecutorNames.RejectionCompletion)
{
    public override async ValueTask<WorkspaceSuggestionWorkflowState> HandleAsync(
        WorkspaceSuggestionWorkflowState workspaceSuggestionWorkflowState,
        IWorkflowContext workflowContext,
        CancellationToken cancellationToken = default)
    {
        // The run is over, so the credential it borrowed is given up here — before the ending is announced, and
        // long before the call's scope is disposed. A later stage that reached for it would find nothing, which is
        // the point: nothing after an ending is entitled to the caller's authority.
        accessTokens.Release();

        workspaceSuggestionWorkflowState.RunStatus = WorkspaceSuggestionRunStatus.Rejected;

        await PublishStreamEventAsync(
            workflowContext,
            WorkspaceSuggestionStreamEventMapper.RunCompleted(workspaceSuggestionWorkflowState, runUsage.Total),
            cancellationToken);

        return workspaceSuggestionWorkflowState;
    }
}
