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
            .AddEdge(executors.Retriever, executors.ProductPool)
            .AddEdge(executors.ProductPool, executors.Reranker)
            .AddEdge(executors.Reranker, executors.Composer)
            .AddEdge(executors.Composer, executors.Validator)
            .AddSwitch(executors.Validator, validationSwitch => validationSwitch
                .AddCase<WorkspaceSuggestionWorkflowState>(
                    state => state?.IsWorkspaceSetupStructureValid is true, [executors.Reviewer])
                .WithDefault([executors.RetryDecision]))
            .AddSwitch(executors.Reviewer, reviewSwitch => reviewSwitch
                .AddCase<WorkspaceSuggestionWorkflowState>(
                    state => state?.WorkspaceSetupReview?.IsAcceptable is true, [executors.Success])
                .WithDefault([executors.RetryDecision]))
            .AddSwitch(executors.RetryDecision, retrySwitch => retrySwitch
                .AddCase<WorkspaceSuggestionWorkflowState>(
                    state => state?.RetryDecision is WorkspaceSetupRetryDecision.RetryWithRephrasing,
                    [executors.Rephraser])
                .WithDefault([executors.Unavailable]))

            // Every node is an output, and the list is the nodes' own: a node added to the record is published
            // without a second edit here, and one cannot be published that the graph does not have.
            .WithOutputFrom(executors.All.ToArray())
            .WithName(WorkflowName)
            .Build();
    }
}
