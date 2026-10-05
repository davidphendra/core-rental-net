using System.Text.Json;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

namespace CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;

/// <summary>Reads the provider's streamed answer into the application's own answer, and nothing else.</summary>
/// <remarks>
/// <para>
/// <b>The provider's wire shape stops here.</b> Everything downstream of this type is the application's
/// vocabulary, so the adapter and the run never name a provider record. The answer is found by what the events
/// carry rather than where they sit: every <c>candidate</c> event is one approved setup, and the run's ending is
/// the <c>completed</c> event, which is deliberately separate from the setups it ends.
/// </para>
/// <para>
/// <b>This reads the ending and only the ending.</b> The stages, retries and approved setups are progress, and
/// progress is read while the stream is still arriving by <see cref="WorkspaceSuggestionProgressReader"/> — which
/// is what lets the panel show where a run is instead of showing everything once the run is over.
/// </para>
/// </remarks>
internal static class WorkspaceSuggestionFragmentReader
{
    /// <summary>The answer, or <c>null</c> when the stream carried no ending.</summary>
    public static WorkspaceSuggestionAnswer? ReadAnswer(string payload)
    {
        var streamEvents = EventStream(WorkspaceSuggestionAnswerObjects.Parse(payload));
        var runEnding = streamEvents.LastOrDefault(
            streamEvent => streamEvent.Type is WorkspaceSuggestionStreamEventType.Completed);

        if (runEnding is null)
        {
            return null;
        }

        return new WorkspaceSuggestionAnswer(
            StatusOf(runEnding.RunStatus),
            ApprovedCandidatesOf(streamEvents),
            RunUsageOf(runEnding.RunUsage));
    }

    /// <summary>Every streamed event the answer carried, in the order it arrived.</summary>
    private static IReadOnlyList<WorkspaceSuggestionStreamEvent> EventStream(
        IReadOnlyList<JsonElement> responseObjects)
        => [.. responseObjects
            .Select(WorkspaceSuggestionAnswerObjects.Read<WorkspaceSuggestionStreamEvent>)
            .OfType<WorkspaceSuggestionStreamEvent>()];

    /// <summary>The agent's run status, in the application's own words.</summary>
    private static WorkspaceSuggestionAnswerStatus StatusOf(string? runStatus)
        => runStatus switch
        {
            "success" => WorkspaceSuggestionAnswerStatus.Suggested,
            "rejected" => WorkspaceSuggestionAnswerStatus.NotWorkspace,
            _ => WorkspaceSuggestionAnswerStatus.CatalogueUnavailable,
        };

    /// <summary>Every approved setup the stream carried, in the order it was approved.</summary>
    private static IReadOnlyList<WorkspaceSuggestionCandidate> ApprovedCandidatesOf(
        IReadOnlyList<WorkspaceSuggestionStreamEvent> streamEvents)
        => [.. streamEvents
            .Where(streamEvent => streamEvent.Type is WorkspaceSuggestionStreamEventType.Candidate
                && streamEvent.ApprovedWorkspaceSetup is not null)
            .Select(streamEvent => WorkspaceSuggestionCandidateReader.From(streamEvent.ApprovedWorkspaceSetup!))];

    /// <summary>The run's cost, in the application's own words, or null when the run did not report one.</summary>
    private static WorkspaceSuggestionRunUsage? RunUsageOf(WorkspaceSuggestionAgentRunUsage? runUsage)
        => runUsage is null
            ? null
            : new WorkspaceSuggestionRunUsage(
                runUsage.ModelCalls,
                runUsage.InputTokens,
                runUsage.OutputTokens,
                runUsage.Model,
                runUsage.PromptVersion);
}
