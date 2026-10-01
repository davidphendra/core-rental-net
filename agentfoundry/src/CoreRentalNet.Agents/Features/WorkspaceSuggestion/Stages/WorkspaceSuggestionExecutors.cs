using Microsoft.Agents.AI.Workflows;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages;

/// <summary>The nodes of the workspace workflow, as data.</summary>
/// <remarks>
/// <para>
/// <b>One list of nodes, so the graph's edges and its outputs cannot name different sets.</b> The graph is wired
/// from these, and <see cref="All"/> is what the outputs are declared from - so a node added here is a node the
/// graph already knows about, and an executor whose events were silently dropped is not a state this can reach.
/// </para>
/// <para>
/// Data, not behaviour: every member is an <see cref="ExecutorBinding"/>, and nothing here decides what runs.
/// </para>
/// </remarks>
internal sealed record WorkspaceSuggestionExecutors(
    ExecutorBinding Input,
    ExecutorBinding Verifier,
    ExecutorBinding Rephraser,
    ExecutorBinding Retriever,
    ExecutorBinding ProductPool,
    ExecutorBinding Reranker,
    ExecutorBinding Composer,
    ExecutorBinding Validator,
    ExecutorBinding Reviewer,
    ExecutorBinding RetryDecision,
    ExecutorBinding Success,
    ExecutorBinding Rejected,
    ExecutorBinding Unavailable)
{
    /// <summary>Every node, in the order the graph declares them as outputs.</summary>
    public IReadOnlyList<ExecutorBinding> All { get; } =
    [
        Input, Verifier, Rephraser, Retriever, ProductPool, Reranker, Composer,
        Validator, Reviewer, RetryDecision, Success, Rejected, Unavailable,
    ];
}
