using System.Diagnostics;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Logging;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Events;
using CoreRentalNet.Agents.Shared.Serialization;
using CoreRentalNet.Agents.Shared.Telemetry;
using CoreRentalNet.Agents.Shared.Workflows;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Executors;

/// <summary>One stage of the workspace pipeline, announcing itself around its work.</summary>
/// <remarks>
/// <para>
/// The state and the event union are fixed once here, so every workspace stage publishes through the union and
/// none of them has to remember that it must.
/// </para>
/// <para>
/// <b>This is also the one place every node is timed.</b> A node's span and its duration and count are recorded
/// here, around <see cref="ProcessAsync"/>, because a node that overrode it could run without a span and a node
/// that forgot to call through would finish a node it never started. A model-backed node's model call is recorded
/// one layer down, in the telemetry chat client.
/// </para>
/// <para>
/// It also brackets every node's work with one console line before and one after, so a run can be followed from
/// the server's side and not only from the caller's stream. The closing line carries the node's own result. The
/// Input entry node does not derive from here - it speaks the chat protocol through its own base - so it writes
/// its own line.
/// </para>
/// </remarks>
internal abstract class WorkspaceSuggestionStreamingExecutor(
    string executorName,
    ILogger logger,
    bool modelBacked = false)
    : StreamingStageExecutor<WorkspaceSuggestionWorkflowState, WorkspaceSuggestionStreamEvent>(executorName)
{
    /// <summary>Announces a node, runs its work, and announces that it finished.</summary>
    public sealed override async ValueTask<WorkspaceSuggestionWorkflowState> HandleAsync(
        WorkspaceSuggestionWorkflowState workspaceSuggestionWorkflowState,
        IWorkflowContext workflowContext,
        CancellationToken cancellationToken = default)
    {
        using var activity = WorkspaceTelemetry.ActivitySource.StartActivity(Id);
        activity?.SetTag(WorkspaceTelemetry.NodeKind, NodeKind);
        activity?.SetTag(WorkspaceTelemetry.RunAttempts, workspaceSuggestionWorkflowState.NextAttemptNumber - 1);

        logger.LogInformation("Node {Node} started.", Id);

        var stopwatch = Stopwatch.StartNew();
        var status = "ok";

        try
        {
            var completedState = await ProcessAsync(workspaceSuggestionWorkflowState, workflowContext, cancellationToken);

            // The closing line carries what the node decided, serialized with the contracts' own options so a
            // result reads on the console exactly as it reads on the wire.
            logger.LogInformation(
                "Node {Node} completed. Result: {Result}",
                Id,
                ContractJson.Serialize(ResultOf(completedState)));

            // A terminal status can only have been written by a completion node, so the run's outcome is read
            // once, here, rather than a reader repeated per ending.
            if (completedState.RunStatus != WorkspaceSuggestionRunStatus.Processing)
            {
                WorkspaceWorkflowTelemetry.RecordRunOutcome(completedState);
            }

            return completedState;
        }
        catch (Exception exception)
        {
            status = "error";
            activity?.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            var tags = new TagList
            {
                { WorkspaceTelemetry.Stage, Id },
                { WorkspaceTelemetry.NodeKind, NodeKind },
                { WorkspaceTelemetry.NodeStatus, status },
            };
            WorkspaceTelemetry.NodeDuration.Record(stopwatch.Elapsed.TotalMilliseconds, tags);
            WorkspaceTelemetry.NodeCount.Add(1, tags);
        }
    }

    /// <summary>Whether this node makes a model call, so the time is attributed to the model or to the code.</summary>
    private string NodeKind => modelBacked ? WorkspaceTelemetry.NodeKindModel : WorkspaceTelemetry.NodeKindDeterministic;

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
