using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent.Events;

namespace CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;

/// <summary>Reads a run's progress out of the agent's answer while the answer is still arriving.</summary>
/// <remarks>
/// An interface so the adapter depends on what a reader does, not on which one it is. <b>One reader belongs to one
/// run:</b> it keeps a cursor, and a cursor shared between two runs would report one run's events into the other's
/// stream — which is why it is registered scoped and the adapter that asks for it is scoped with it.
/// </remarks>
public interface IWorkspaceSuggestionProgressReader
{
    /// <summary>The progress events that closed since the previous call, in the order they arrived.</summary>
    IReadOnlyList<WorkspaceSuggestionEvent> Read(string streamedText);
}
