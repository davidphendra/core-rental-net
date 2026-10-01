using Microsoft.Agents.AI.Workflows;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Routing;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.StreamEventPublishing;
using CoreRentalNet.Agents.Shared.Mcp;
using CoreRentalNet.Agents.Shared.Model;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.StageExecutors;

/// <summary>Ends a run whose attempts are exhausted, or whose catalogue could not be searched.</summary>
internal sealed class WorkspaceSuggestionUnavailableCompletionExecutor(
    AgentRunUsageAccumulator runUsage,
    IMcpAccessTokenService accessTokens)
    : WorkspaceSuggestionStreamingStageExecutor(WorkspaceWorkflowExecutorNames.UnavailableCompletion)
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

        workspaceSuggestionWorkflowState.RunStatus = WorkspaceSuggestionRunStatus.Unavailable;

        await PublishStreamEventAsync(
            workflowContext,
            WorkspaceSuggestionStreamEventMapper.RunCompleted(workspaceSuggestionWorkflowState, runUsage.Total),
            cancellationToken);

        return workspaceSuggestionWorkflowState;
    }
}
