using Microsoft.Agents.AI;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion;

/// <summary>The factory that publishes a fresh workflow agent for one request.</summary>
/// <remarks>
/// A port rather than a class, so the composition root names the pipeline without naming the shape it has
/// today. The build takes nothing: the call's credential and the catalogue it entitles belong to the model
/// call, not to the pipeline, and <see cref="CoreRentalNet.Agents.Shared.ChatClients.AuthorisedMcpChatClient"/> applies them there.
/// Each call returns a new agent and a new workflow; the factory holds no run state.
/// </remarks>
internal interface IWorkspaceSuggestionWorkflow
{
    /// <summary>A new workflow agent for one request, ready for the host to serve and to checkpoint.</summary>
    AIAgent AsAIAgent();
}
