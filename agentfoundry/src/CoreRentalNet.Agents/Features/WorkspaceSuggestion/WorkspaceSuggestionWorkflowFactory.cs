using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using Microsoft.Agents.AI.Workflows;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion;

/// <summary>Builds the workspace workflow graph, once per request.</summary>
/// <remarks>
/// <para>
/// A coordinator and nothing more: the nodes belong to <see cref="WorkspaceSuggestionExecutorBuilder"/>, the
/// topology belongs to <see cref="WorkspaceSuggestionWorkflowGraph"/>, and this says only that a run is one and
/// then the other. It used to do all three, which is why a new stage and a new cross-cutting concern both landed
/// in the same file.
/// </para>
/// <para>
/// The name is kept because the composition root and the served workflow resolve it by it.
/// </para>
/// </remarks>
internal sealed class WorkspaceSuggestionWorkflowFactory()
{
    public const string WorkflowName = "workspace-suggestion-workflow";

    /// <summary>The workflow those nodes make, wired and named.</summary>
    public static Workflow Create(WorkspaceSuggestionExecutorBuilder executorBuilder)
    {
        ArgumentNullException.ThrowIfNull(executorBuilder);

        var executors = executorBuilder.Build();

        return new WorkflowBuilder(executors.Input)
            .AddEdge(executors.Input, executors.Verifier)
            .AddSwitch(executors.Verifier, verificationSwitch => verificationSwitch
                .AddCase<WorkspaceSuggestionWorkflowState>(
                    state => state?.RequestVerification?.IsWorkspaceRequest is true, [executors.Rephraser])
                .WithDefault([executors.Rejected]))
            .AddEdge(executors.Rephraser, executors.Retriever)
            .AddSwitch(executors.Retriever, retrievalSwitch => retrievalSwitch
                .AddCase<WorkspaceSuggestionWorkflowState>(
                    state => state?.CatalogueRetrieval?.IsAvailable is false, [executors.Unavailable])
                .WithDefault([executors.ProductPool]))
            .AddSwitch(executors.ProductPool, poolSwitch => poolSwitch
                .AddCase<WorkspaceSuggestionWorkflowState>(
                    state => state?.CandidatePool.Count is 0, [executors.Unavailable])
                .WithDefault([executors.Reranker]))
            .AddEdge(executors.Reranker, executors.Composer)
            .AddEdge(executors.Composer, executors.Success)

            // The composer materialises the reranker result directly; the selection policy is the only
            // post-reranker validation.
            .WithOutputFrom(executors.All.ToArray())
            .WithName(WorkflowName)
            .Build();
    }
}
