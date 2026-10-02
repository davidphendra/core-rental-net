using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Logging;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Routing;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.StageExecutors;

/// <summary>Reads the sentence again, using only the previous attempt's issues when there were any.</summary>
/// <remarks>
/// <b>The reading is bounded before it is kept.</b> A model that returned more words than a search accepts would
/// otherwise reach the retriever, be refused at the tool boundary, and end a run over a cosmetic overrun — and
/// the refusal's sentence does not cross back to say so. The policy is the reason that path is unreachable from
/// this pipeline rather than a hope that the prompt was obeyed.
/// </remarks>
internal sealed class WorkspaceRequirementRephrasingExecutor(
    AIAgent stageAgent,
    WorkspaceComponentSearchVocabularyLimitPolicy vocabularyLimitPolicy,
    ILogger logger)
    : WorkspaceSuggestionStreamingStageExecutor(WorkspaceWorkflowExecutorNames.Rephraser, logger)
{
    protected override async ValueTask<WorkspaceSuggestionWorkflowState> ProcessAsync(
        WorkspaceSuggestionWorkflowState workspaceSuggestionWorkflowState,
        IWorkflowContext workflowContext,
        CancellationToken cancellationToken = default)
    {
        await RunProcessingStageAsync(
            workspaceSuggestionWorkflowState,
            workflowContext,
            WorkspaceProcessingStage.RephrasingRequirement,
            async stageCancellationToken =>
            {
                var agentResponse = await stageAgent.RunAsync<WorkspaceRequirementExpansion>(
                    WorkspaceStageInput.Rephrasing(workspaceSuggestionWorkflowState),
                    cancellationToken: stageCancellationToken);

                workspaceSuggestionWorkflowState.RequirementExpansion =
                    vocabularyLimitPolicy.ApplySearchVocabularyLimits(agentResponse.Result);
            },
            cancellationToken);

        return workspaceSuggestionWorkflowState;
    }
}
