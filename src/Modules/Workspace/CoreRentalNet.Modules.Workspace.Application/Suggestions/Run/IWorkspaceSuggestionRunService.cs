using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run.Events;

namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;

/// <summary>The one place a suggestion run is executed: the service the endpoint delegates to.</summary>
public interface IWorkspaceSuggestionRunService
{
    /// <summary>Runs one suggestion and yields what happens while it happens.</summary>
    /// <remarks>
    /// <b>Enumerating this is what runs it.</b> Nothing happens until the first <c>MoveNextAsync</c>, which is
    /// what lets the endpoint open the stream before the first token is spent - and it is what makes abandoning
    /// the enumeration an ending: the run is written whichever way the caller stopped reading.
    /// </remarks>
    IAsyncEnumerable<WorkspaceSuggestionStreamEventBase> StreamAsync(
        WorkspaceSuggestionRequestPayload suggestionRequestPayload,
        string hashedCustomerIdentity,
        CancellationToken cancellationToken);
}
