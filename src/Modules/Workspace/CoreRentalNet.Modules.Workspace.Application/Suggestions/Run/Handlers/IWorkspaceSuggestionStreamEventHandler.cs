using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent.Events;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run.Events;

namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run.Handlers;

/// <summary>One link in the run's stream chain: it handles the event it owns and passes on the rest.</summary>
/// <remarks>
/// <para>
/// <c>TryHandle</c> rather than <c>CanHandle</c> plus <c>Handle</c>, so a link has one public operation and
/// the processor asks it exactly once per event. A link is stateless, so it is registered as a singleton and
/// the run's state travels in the call rather than living on the handler.
/// </para>
/// <para>
/// <b>A link produces frames rather than writing them.</b> The endpoint owns the transport - it opens the
/// stream and puts each frame on the wire - so what a handler knows is what is worth saying, never how it is
/// framed. Nothing a link does waits on anything, which is why this is synchronous: the run's two waits are the
/// agent's stream and the endpoint's writes, and both are outside it.
/// </para>
/// </remarks>
public interface IWorkspaceSuggestionStreamEventHandler
{
    /// <summary>Applies the event if it is this handler's, and reports the frames it produced - often none.</summary>
    /// <returns><c>true</c> when the event was this handler's, so the chain stops asking the rest.</returns>
    bool TryHandleAgentEvent(
        WorkspaceSuggestionEvent suggestionEvent,
        WorkspaceSuggestionRunState suggestionRunState,
        ICollection<WorkspaceSuggestionStreamEventBase> frames);
}
