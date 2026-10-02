using System.Runtime.CompilerServices;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent.Events;

namespace CoreRentalNet.Host.Tests.WorkspaceSuggestion;

/// <summary>An agent that says exactly what a test scripted, so the endpoint's framing can be asserted.</summary>
internal sealed class ScriptedSuggestionAgentAdapter(params WorkspaceSuggestionEvent[] agentEvents)
    : IWorkspaceSuggestionAgentAdapter
{
    public async IAsyncEnumerable<WorkspaceSuggestionEvent> StreamAsync(
        WorkspaceSuggestionRequestPayload suggestionRequestPayload,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var agentEvent in agentEvents)
        {
            await Task.Yield();

            yield return agentEvent;
        }
    }
}
