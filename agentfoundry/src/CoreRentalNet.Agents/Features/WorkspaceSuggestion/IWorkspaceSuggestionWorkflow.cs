using Microsoft.Agents.AI;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion;

/// <summary>The pipeline this deployable serves, published as the one agent the host registers.</summary>
/// <remarks>
/// A port rather than a class, so the composition root names the pipeline without naming the shape it has
/// today. The build takes nothing: the call's credential and the catalogue it entitles belong to the model
/// call, not to the pipeline, and <see cref="CoreRentalNet.Agents.Shared.ChatClients.AuthorisedMcpChatClient"/> applies them there.
/// </remarks>
internal interface IWorkspaceSuggestionWorkflow
{
    /// <summary>The pipeline as one agent, ready for the host to serve and to checkpoint.</summary>
    AIAgent AsAIAgent();
}
