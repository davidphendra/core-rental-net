using System.Runtime.CompilerServices;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent.Events;

namespace CoreRentalNet.Modules.Workspace.UnitTests.Suggestions;

/// <summary>A hand-written agent: it yields exactly the events a test scripted, in order.</summary>
internal sealed class ScriptedWorkspaceSuggestionAgentAdapter(
    IReadOnlyList<WorkspaceSuggestionEvent> scriptedEvents) : IWorkspaceSuggestionAgentAdapter
{
    public async IAsyncEnumerable<WorkspaceSuggestionEvent> StreamAsync(
        WorkspaceSuggestionRequestPayload suggestionRequestPayload,
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
