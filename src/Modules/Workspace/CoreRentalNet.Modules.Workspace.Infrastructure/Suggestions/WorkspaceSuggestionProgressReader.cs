using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent.Events;

namespace CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;

/// <summary>Turns the agent's streamed JSON into progress the moment each event closes, and nothing else.</summary>
/// <remarks>
/// <para>
/// The provider says where a run is as it goes — one typed JSON object per line — so the caller can be shown a
/// stage, a retry or an approved setup while the run is still going. Reading the whole text only when the stream
/// ends, which is all the ending needs, held every one of those back until the run was over.
/// </para>
/// <para>
/// Rescanning from the start and keeping a cursor is deliberate, exactly as the narrative field reader used to do:
/// the answer is a couple of kilobytes, the scanner stops cleanly at an incomplete object, and resuming would be
/// bookkeeping for no gain. The ending is not progress — a <c>completed</c> event is read from the whole text by
/// <see cref="WorkspaceSuggestionFragmentReader"/> after the stream has stopped.
/// </para>
/// </remarks>
public sealed class WorkspaceSuggestionProgressReader : IWorkspaceSuggestionProgressReader
{
    private int _emitted;

    public IReadOnlyList<WorkspaceSuggestionEvent> Read(string streamedText)
    {
        // Captured before the cursor moves: Skip reads its argument when it is called, so assigning _emitted
        // first would skip the very objects this call exists to report.
        var complete = WorkspaceSuggestionAnswerObjects.Parse(streamedText);
        var alreadyEmitted = _emitted;

        _emitted = complete.Count;

        return [.. complete
            .Skip(alreadyEmitted)
            .Select(WorkspaceSuggestionAnswerObjects.Read<WorkspaceSuggestionStreamEvent>)
            .OfType<WorkspaceSuggestionStreamEvent>()
            .Select(ProgressOf)
            .OfType<WorkspaceSuggestionEvent>()];
    }

    /// <summary>The event one streamed object is, or null when it is not progress.</summary>
    private static WorkspaceSuggestionEvent? ProgressOf(WorkspaceSuggestionStreamEvent streamEvent)
        => streamEvent.Type switch
        {
            WorkspaceSuggestionStreamEventType.StageStarted when streamEvent.ProcessingStage is not null
                => new WorkspaceSuggestionStageStartedEvent(streamEvent.ProcessingStage),
            WorkspaceSuggestionStreamEventType.StageCompleted when streamEvent.ProcessingStage is not null
                => new WorkspaceSuggestionStageCompletedEvent(streamEvent.ProcessingStage),
            WorkspaceSuggestionStreamEventType.Retry
                => new WorkspaceSuggestionRetryEvent(
                    streamEvent.NextAttemptNumber, streamEvent.MaximumAttemptCount),
            WorkspaceSuggestionStreamEventType.Candidate when streamEvent.ApprovedWorkspaceSetup is not null
                => new WorkspaceSuggestionCandidateApprovedEvent(
                    WorkspaceSuggestionCandidateReader.From(streamEvent.ApprovedWorkspaceSetup)),
            _ => null,
        };
}
