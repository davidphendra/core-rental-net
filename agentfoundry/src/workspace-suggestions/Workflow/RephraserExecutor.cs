using AgentFoundry.WorkspaceSuggestions.Specifications;
using AgentFoundry.WorkspaceSuggestions.Vocabularies;
using Microsoft.Agents.AI.Workflows;

namespace AgentFoundry.WorkspaceSuggestions.Workflows;

/// <summary>
/// The run's second node: it turns a verified request into the specification everything else works from.
/// </summary>
/// <remarks>
/// Findings arrive here on a retry rather than at a node of their own, because the component that wrote
/// attempt one is the component that must write attempt two - two authors of one specification is how
/// the two come to disagree about what a slot is called. The findings are empty on the first attempt
/// and carried on every later one.
/// </remarks>
internal sealed class RephraserExecutor(IRephraseRequests rephraser)
    : Executor<Verification, Specification>(Stages.Rephrasing)
{
    /// <summary>Nothing objects on the first attempt, which is what the run starts with.</summary>
    private static readonly IReadOnlyList<Contracts.Finding> NoFindings = [];

    public override async ValueTask<Specification> HandleAsync(
        Verification message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        return await rephraser.RephraseAsync(message.Request, NoFindings, cancellationToken);
    }
}
