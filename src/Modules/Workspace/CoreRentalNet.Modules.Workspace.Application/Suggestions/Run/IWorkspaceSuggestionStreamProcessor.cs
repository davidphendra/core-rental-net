using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;

/// <summary>Applies an agent's whole stream to one run, through the chain of handlers.</summary>
public interface IWorkspaceSuggestionStreamProcessor
{
    /// <summary>Yields each frame the run produces, in the order the run produced it.</summary>
    /// <remarks>
    /// An enumeration rather than a callback, so the run is a sequence the caller pulls from: the endpoint relays
    /// it to the response, and a test reads it as the frames it is. Nothing here knows a transport exists.
    /// </remarks>
    IAsyncEnumerable<WorkspaceSuggestionStreamEvent> ProcessAsync(
        IAsyncEnumerable<WorkspaceSuggestionEvent> suggestionAgentEvents,
        WorkspaceSuggestionRunState suggestionRunState,
        CancellationToken cancellationToken);
}
