using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Logging;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Routing;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.StageExecutors;

/// <summary>Rejects a setup the catalogue could not honour, before a model is asked to judge it.</summary>
/// <remarks>
/// Every setup in the set is checked, and the set passes only when all of them do: a run whose second setup names
/// a product no search returned is not a run whose first setup may be shown as if the set were sound.
/// </remarks>
internal sealed class WorkspaceSetupCandidateValidationExecutor(
    WorkspaceSetupCandidateStructureValidator structureValidator,
    ILogger logger)
    : WorkspaceSuggestionStreamingStageExecutor(WorkspaceWorkflowExecutorNames.Validator, logger)
{
    protected override async ValueTask<WorkspaceSuggestionWorkflowState> ProcessAsync(
        WorkspaceSuggestionWorkflowState workspaceSuggestionWorkflowState,
        IWorkflowContext workflowContext,
        CancellationToken cancellationToken = default)
    {
        await RunProcessingStageAsync(
            workspaceSuggestionWorkflowState,
            workflowContext,
            WorkspaceProcessingStage.ValidatingWorkspaceSetups,
            stageCancellationToken =>
            {
                var isStructurallyValid = workspaceSuggestionWorkflowState.ProposedWorkspaceSetups.Count > 0;

                foreach (var workspaceSetupCandidate in workspaceSuggestionWorkflowState.ProposedWorkspaceSetups)
                {
                    isStructurallyValid &= structureValidator.IsStructurallyValid(
                        workspaceSetupCandidate,
                        workspaceSuggestionWorkflowState.RequirementExpansion!,
                        workspaceSuggestionWorkflowState.SlotCapacityRules,
                        workspaceSuggestionWorkflowState.SelectedWorkspaceCandidates,
                        out _);
                }

                workspaceSuggestionWorkflowState.IsWorkspaceSetupStructureValid = isStructurallyValid;

                return ValueTask.CompletedTask;
            },
            cancellationToken);

        return workspaceSuggestionWorkflowState;
    }
}
