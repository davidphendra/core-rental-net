using System.Runtime.CompilerServices;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

namespace CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;

/// <summary>What is registered when the feature is off or the endpoint is missing.</summary>
/// <remarks>
/// It yields <b>unavailable</b>, never a suggestion and never a refusal - the deployment has not been told
/// where the agent is, which is not the customer's problem. The suggestion panel is hidden in this state; this
/// implementation exists so that nothing downstream has to check whether the feature is on.
/// </remarks>
public sealed class UnconfiguredWorkspaceSuggestionAgentAdapter : IWorkspaceSuggestionAgentAdapter
{
    public async IAsyncEnumerable<WorkspaceSuggestionAgentEvent> StreamSuggestionAsync(
        WorkspaceSuggestionRequestPayload suggestionRequestPayload,
        string callerAccessToken,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        yield return new WorkspaceSuggestionUnavailableAgentEvent("No suggestion agent is configured.");
    }
}
