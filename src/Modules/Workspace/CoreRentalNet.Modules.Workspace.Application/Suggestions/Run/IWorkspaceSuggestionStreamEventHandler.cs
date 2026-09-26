using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;

/// <summary>One link in the run's stream chain: it handles the event it owns and passes on the rest.</summary>
/// <remarks>
/// <c>TryHandle</c> rather than <c>CanHandle</c> plus <c>Handle</c>, so a link has one public operation and
/// the processor asks it exactly once per event. A link is stateless, so it is registered as a singleton and
/// the run's state travels in the call rather than living on the handler.
/// </remarks>
public interface IWorkspaceSuggestionStreamEventHandler
{
    /// <summary>Applies the event and reports whether it belonged to this handler.</summary>
    Task<bool> TryHandleAgentEventAsync(
        WorkspaceSuggestionAgentEvent suggestionAgentEvent,
        WorkspaceSuggestionRunState suggestionRunState,
        IWorkspaceSuggestionEventWriter suggestionEventWriter,
        CancellationToken cancellationToken);
}
