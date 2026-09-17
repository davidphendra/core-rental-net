using AgentFoundry.WorkspaceSuggestions.Selection;
using AgentFoundry.WorkspaceSuggestions.Specifications;
using AgentFoundry.WorkspaceSuggestions.Vocabularies;
using Microsoft.Agents.AI.Workflows;

namespace AgentFoundry.WorkspaceSuggestions.Workflows;

/// <summary>
/// The run's third node, and the only one that reads the catalogue.
/// </summary>
/// <remarks>
/// It reads once per run and groups locally, so the endpoint is asked once and no slot sees a different
/// set from any other - which is what keeps the three tiers comparable. Its executor id is the stage id,
/// so the event the framework raises is already the message the contract streams.
/// </remarks>
internal sealed class SuggestorExecutor(ISelectCandidates suggestor)
    : Executor<Specification, Candidates>(Stages.Selecting)
{
    public override async ValueTask<Candidates> HandleAsync(
        Specification message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var options = await suggestor.SelectAsync(message, cancellationToken);

        return new Candidates(message, options);
    }
}
