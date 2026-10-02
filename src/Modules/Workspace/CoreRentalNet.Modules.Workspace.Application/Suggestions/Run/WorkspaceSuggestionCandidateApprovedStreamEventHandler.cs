using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent.Events;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run.Events;

namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;

/// <summary>Reports an approved setup as a frame of its own, so the panel can count it as it arrives.</summary>
/// <remarks>
/// The setup itself is rendered from the terminal outcome frame, which carries every approved setup; this frame
/// is what tells the customer that something was found before the run ends.
/// </remarks>
public sealed class WorkspaceSuggestionCandidateApprovedStreamEventHandler : IWorkspaceSuggestionStreamEventHandler
{
    public bool TryHandleAgentEvent(
        WorkspaceSuggestionEvent suggestionEvent,
        WorkspaceSuggestionRunState suggestionRunState,
        ICollection<WorkspaceSuggestionStreamEvent> frames)
    {
        if (suggestionEvent is not WorkspaceSuggestionCandidateApprovedEvent approvedCandidateAgentEvent)
        {
            return false;
        }

        frames.Add(new WorkspaceSuggestionCandidateStreamEvent(
            $"Workspace setup found: {approvedCandidateAgentEvent.Candidate.Rationale}"));

        return true;
    }
}
