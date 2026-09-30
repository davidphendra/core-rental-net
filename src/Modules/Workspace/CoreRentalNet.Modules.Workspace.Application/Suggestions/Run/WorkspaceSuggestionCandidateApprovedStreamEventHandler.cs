using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;

/// <summary>Writes an approved setup as a frame of its own, so the panel can count it as it arrives.</summary>
/// <remarks>
/// The setup itself is rendered from the terminal outcome frame, which carries every approved setup; this frame
/// is what tells the customer that something was found before the run ends.
/// </remarks>
public sealed class WorkspaceSuggestionCandidateApprovedStreamEventHandler : IWorkspaceSuggestionStreamEventHandler
{
    public async Task<bool> TryHandleAgentEventAsync(
        WorkspaceSuggestionAgentEvent suggestionAgentEvent,
        WorkspaceSuggestionRunState suggestionRunState,
        IWorkspaceSuggestionEventWriter suggestionEventWriter,
        CancellationToken cancellationToken)
    {
        if (suggestionAgentEvent is not WorkspaceSuggestionCandidateApprovedAgentEvent approvedCandidateAgentEvent)
        {
            return false;
        }

        await suggestionEventWriter.WriteAsync(
            new WorkspaceSuggestionCandidateStreamEvent(
                $"Workspace setup found: {approvedCandidateAgentEvent.Candidate.Rationale}"),
            cancellationToken);

        return true;
    }
}
