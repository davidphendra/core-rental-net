using System.Runtime.CompilerServices;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent.Events;

namespace CoreRentalNet.Host.Tests.WorkspaceSuggestion;

/// <summary>An agent whose own call is cancelled after it has already reported a stage.</summary>
/// <remarks>
/// A cancellation mid-stream is the case worth pinning: a frame the customer has already read must survive it,
/// and the stream must end rather than the connection being aborted with the run half written.
/// </remarks>
internal sealed class CancellingSuggestionAgentAdapter : IWorkspaceSuggestionAgentAdapter
{
    public async IAsyncEnumerable<WorkspaceSuggestionEvent> StreamAsync(
        WorkspaceSuggestionRequestPayload suggestionRequestPayload,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();

        yield return new WorkspaceSuggestionChangedEvent("verifyingRequest");

        throw new OperationCanceledException("The agent's call was cancelled.");
    }
}
