using System.Runtime.CompilerServices;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

namespace CoreRentalNet.Host.Tests.WorkspaceSuggestion;

/// <summary>An agent that keeps the token the run was handed, then ends the run.</summary>
/// <remarks>
/// The run endpoint reads the caller's token through the identity SDK; this adapter is how a test sees which
/// token actually reached the run, rather than only that a run happened.
/// </remarks>
internal sealed class CapturingSuggestionAgentAdapter : IWorkspaceSuggestionAgentAdapter
{
    /// <summary>The token the last run was handed, or null when no run reached the adapter.</summary>
    public string? CallerAccessToken { get; private set; }

    public async IAsyncEnumerable<WorkspaceSuggestionAgentEvent> StreamSuggestionAsync(
        WorkspaceSuggestionRequestPayload suggestionRequestPayload,
        string callerAccessToken,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        CallerAccessToken = callerAccessToken;

        await Task.Yield();

        yield return new WorkspaceSuggestionUnavailableAgentEvent("captured");
    }
}
