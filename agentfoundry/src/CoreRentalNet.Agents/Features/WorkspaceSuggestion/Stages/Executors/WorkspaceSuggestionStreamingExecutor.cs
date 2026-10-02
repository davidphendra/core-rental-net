using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Logging;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.StreamingEvents;
using CoreRentalNet.Agents.Shared.Serialization;
using CoreRentalNet.Agents.Shared.Workflows;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Executors;

/// <summary>One stage of the workspace pipeline, announcing itself around its work.</summary>
/// <remarks>
/// <para>
/// The state and the event union are fixed once here, so every workspace stage publishes through the union and
/// none of them has to remember that it must.
/// </para>
/// <para>
/// It also brackets every node's work with one console line before and one after, so a run can be followed from
/// the server's side and not only from the caller's stream. The closing line carries the node's own result. The
/// Input entry node does not derive from here - it speaks the chat protocol through its own base - so it writes
/// its own line.
/// </para>
/// </remarks>
internal abstract class WorkspaceSuggestionStreamingExecutor(string executorName, ILogger logger)
    : StreamingStageExecutor<WorkspaceSuggestionWorkflowState, WorkspaceSuggestionStreamEvent>(executorName)
{
    /// <summary>Announces a node, runs its work, and announces that it finished.</summary>
    /// <remarks>
    /// Sealed, because it is the one place every node is bracketed: a node that overrode it could run without a
    /// line, and a node that forgot to call through would complete a node it never started. A node writes its
    /// behaviour in <see cref="ProcessAsync"/> instead.
    /// </remarks>
    public sealed override async ValueTask<WorkspaceSuggestionWorkflowState> HandleAsync(
        WorkspaceSuggestionWorkflowState workspaceSuggestionWorkflowState,
        IWorkflowContext workflowContext,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Node {Node} started.", Id);

        var completedState = await ProcessAsync(workspaceSuggestionWorkflowState, workflowContext, cancellationToken);

        // The closing line carries what the node decided, serialized with the contracts' own options so a result
        // reads on the console exactly as it reads on the wire.
        logger.LogInformation(
            "Node {Node} completed. Result: {Result}",
            Id,
            ContractJson.Serialize(ResultOf(completedState)));

        return completedState;
    }

    /// <summary>What this node produced, which is what its completion line reports.</summary>
    /// <remarks>
    /// A node's result is the field it owns on the run's state and nothing else: the state is the shared
    /// accumulator, so logging the whole of it would repeat every earlier node's result at every step. One
    /// abstract member keeps the choice in the node that made it and the writing in the one place that brackets.
    /// </remarks>
    protected abstract object? ResultOf(WorkspaceSuggestionWorkflowState workspaceSuggestionWorkflowState);

    /// <summary>The node's own work, whatever it is.</summary>
    protected abstract ValueTask<WorkspaceSuggestionWorkflowState> ProcessAsync(
        WorkspaceSuggestionWorkflowState workspaceSuggestionWorkflowState,
        IWorkflowContext workflowContext,
        CancellationToken cancellationToken);

    /// <summary>Announces a stage, runs its work, and announces that it finished.</summary>
    protected static async ValueTask RunProcessingStageAsync(
        WorkspaceSuggestionWorkflowState workspaceSuggestionWorkflowState,
        IWorkflowContext workflowContext,
        WorkspaceProcessingStage processingStage,
        Func<CancellationToken, ValueTask> processingStageWork,
        CancellationToken cancellationToken)
    {
        workspaceSuggestionWorkflowState.CurrentProcessingStage = processingStage;

        await PublishStreamEventAsync(
            workflowContext,
            new WorkspaceProcessingStageStartedEvent(
                workspaceSuggestionWorkflowState.CustomerWorkflowIdentifier, processingStage),
            cancellationToken);

        await processingStageWork(cancellationToken);

        await PublishStreamEventAsync(
            workflowContext,
            new WorkspaceProcessingStageCompletedEvent(
                workspaceSuggestionWorkflowState.CustomerWorkflowIdentifier, processingStage),
            cancellationToken);
    }
}
