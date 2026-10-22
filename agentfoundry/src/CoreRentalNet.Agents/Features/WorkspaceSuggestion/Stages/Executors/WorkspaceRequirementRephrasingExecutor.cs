using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Logging;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Routing;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Executors;

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
    WorkspaceRequirementBudgetInvariantPolicy budgetInvariantPolicy,
    WorkspaceAccessoryRelevancePolicy accessoryRelevancePolicy,
    WorkspaceExplicitModelTermPolicy explicitModelTermPolicy,
    ILogger logger)
    : WorkspaceSuggestionStreamingExecutor(WorkspaceWorkflowExecutorNames.Rephraser, logger, modelBacked: true)
{
    protected override object? ResultOf(WorkspaceSuggestionWorkflowState workspaceSuggestionWorkflowState)
        => workspaceSuggestionWorkflowState.RequirementExpansion;

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
                var agentResponse = await stageAgent.RunAsync<WorkspaceRequirementExpansionWire>(
                    WorkspaceStageInput.Rephrasing(workspaceSuggestionWorkflowState),
                    cancellationToken: stageCancellationToken);

                var expansion = WorkspaceRequirementExpansionMapper.Map(
                    agentResponse.Result,
                    workspaceSuggestionWorkflowState.OriginalCustomerQuery);

                workspaceSuggestionWorkflowState.RequirementExpansion =
                    accessoryRelevancePolicy.ApplyRequestedAccessories(
                        budgetInvariantPolicy.ApplyBudgetInvariant(
                            vocabularyLimitPolicy.ApplySearchVocabularyLimits(
                                explicitModelTermPolicy.Apply(expansion)),
                            workspaceSuggestionWorkflowState.CustomerStatedCeilingMonthly));
            },
            cancellationToken);

        return workspaceSuggestionWorkflowState;
    }
}
