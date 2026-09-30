using System.Runtime.CompilerServices;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

namespace CoreRentalNet.Modules.Workspace.UnitTests.Suggestions;

/// <summary>A hand-written agent: it yields exactly the events a test scripted, in order.</summary>
internal sealed class ScriptedWorkspaceSuggestionAgentAdapter(
    IReadOnlyList<WorkspaceSuggestionAgentEvent> scriptedEvents) : IWorkspaceSuggestionAgentAdapter
{
    public async IAsyncEnumerable<WorkspaceSuggestionAgentEvent> StreamSuggestionAsync(
        WorkspaceSuggestionRequestPayload suggestionRequestPayload,
        string callerAccessToken,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        foreach (var scriptedEvent in scriptedEvents)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return scriptedEvent;
        }
    }
}
