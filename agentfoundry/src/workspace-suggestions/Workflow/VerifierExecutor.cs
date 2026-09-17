using AgentFoundry.WorkspaceSuggestions.Contracts;
using AgentFoundry.WorkspaceSuggestions.Intent;
using AgentFoundry.WorkspaceSuggestions.Vocabularies;
using Microsoft.Agents.AI.Workflows;

namespace AgentFoundry.WorkspaceSuggestions.Workflows;

/// <summary>
/// The run's first node, and the cheapest one: it decides whether to answer at all.
/// </summary>
/// <remarks>
/// It runs before anything reads the catalogue, so a request that is not about a workspace costs one
/// short call rather than a whole run. Its executor id is the stage id, so the event the framework
/// raises when this node finishes is already the stage the contract streams - one name, not a name and
/// a mapping that can disagree with it.
/// </remarks>
internal sealed class VerifierExecutor(IIntentClassifier classifier)
    : Executor<SuggestionRequest, Verification>(Stages.Verifying)
{
    public override async ValueTask<Verification> HandleAsync(
        SuggestionRequest message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var verdict = await classifier.ClassifyAsync(message.Query, cancellationToken);

        return new Verification(message, verdict);
    }
}
