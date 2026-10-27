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
/// The prompt owns the category reading and the search vocabulary. The application keeps only the budget
/// invariant and the model-term correction, neither of which decides whether a component is relevant.
/// </remarks>
internal sealed class WorkspaceRequirementRephrasingExecutor(
    AIAgent stageAgent,
    WorkspaceRequirementBudgetInvariantPolicy budgetInvariantPolicy,
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
                    budgetInvariantPolicy.ApplyBudgetInvariant(
                        explicitModelTermPolicy.Apply(expansion),
                        workspaceSuggestionWorkflowState.CustomerStatedCeilingMonthly);
            },
            cancellationToken);

        return workspaceSuggestionWorkflowState;
    }
}
