using System.Text.Json;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

namespace CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;

/// <summary>Reads the provider's streamed answer into the application's own answer, and nothing else.</summary>
/// <remarks>
/// <b>The provider's wire shape stops here.</b> Everything downstream of this type is the application's
/// vocabulary, so the adapter and the run never name a provider record. The answer is found by what the events
/// carry rather than where they sit: every <c>candidate</c> event is one approved setup, and the run's ending is
/// the <c>completed</c> event, which is deliberately separate from the setups it ends.
/// </remarks>
internal static class WorkspaceSuggestionFragmentReader
{
    /// <summary>The answer, or <c>null</c> when the stream carried no ending.</summary>
    public static WorkspaceSuggestionAnswer? ReadAnswer(string payload) => Parse(payload)?.Answer;

    /// <summary>Everything the stream carried: the answer, the stages it ran, and the retries it made.</summary>
    public static WorkspaceSuggestionAnswerDetail? Parse(string payload)
    {
        var responseObjects = WorkspaceSuggestionAnswerObjects.Parse(payload);
        var streamEvents = EventStream(responseObjects);
        var runEnding = streamEvents.LastOrDefault(streamEvent => streamEvent.Type is "completed");

        if (runEnding is null)
        {
            return null;
        }

        return new WorkspaceSuggestionAnswerDetail(
            new WorkspaceSuggestionAnswer(
                StatusOf(runEnding.RunStatus),
                ApprovedCandidatesOf(streamEvents),
                RunUsageOf(runEnding.RunUsage)
            ),
            ProcessingStagesOf(streamEvents),
            RetryAttemptsOf(streamEvents)
        );
    }

    /// <summary>The stages the run announced, in the order it announced them.</summary>
    private static IReadOnlyList<string> ProcessingStagesOf(
        IReadOnlyList<MicrosoftFoundrySuggestionStreamEvent> streamEvents)
        => [.. streamEvents
            .Where(streamEvent => streamEvent.Type is "stageStarted" && streamEvent.ProcessingStage is not null)
            .Select(streamEvent => streamEvent.ProcessingStage!)];

    /// <summary>The retries the run announced, in the order it announced them.</summary>
    private static IReadOnlyList<MicrosoftFoundrySuggestionRetryAttempt> RetryAttemptsOf(
        IReadOnlyList<MicrosoftFoundrySuggestionStreamEvent> streamEvents)
        => [.. streamEvents
            .Where(streamEvent => streamEvent.Type is "retry")
            .Select(streamEvent => new MicrosoftFoundrySuggestionRetryAttempt(
                streamEvent.NextAttemptNumber, streamEvent.MaximumAttemptCount))];

    /// <summary>The run's cost, in the application's own words, or null when the run did not report one.</summary>
    private static WorkspaceSuggestionRunUsage? RunUsageOf(MicrosoftFoundrySuggestionAgentRunUsage? runUsage)
        => runUsage is null
            ? null
            : new WorkspaceSuggestionRunUsage(
                runUsage.ModelCalls,
                runUsage.InputTokens,
                runUsage.OutputTokens,
                runUsage.Model,
                runUsage.PromptVersion);

    /// <summary>Every streamed event the answer carried, in the order it arrived.</summary>
    private static IReadOnlyList<MicrosoftFoundrySuggestionStreamEvent> EventStream(
        IReadOnlyList<JsonElement> responseObjects)
        => [.. responseObjects
            .Where(element => WorkspaceSuggestionAnswerObjects.Has(element, "type"))
            .Select(WorkspaceSuggestionAnswerObjects.Read<MicrosoftFoundrySuggestionStreamEvent>)
            .OfType<MicrosoftFoundrySuggestionStreamEvent>()];

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
        IReadOnlyList<MicrosoftFoundrySuggestionStreamEvent> streamEvents)
        => [.. streamEvents
            .Where(streamEvent => streamEvent.Type is "candidate" && streamEvent.ApprovedWorkspaceSetup is not null)
            .Select(streamEvent => CandidateOf(streamEvent.ApprovedWorkspaceSetup!))];

    /// <summary>One candidate, totalled from the lines the agent stated.</summary>
    /// <remarks>
    /// A line's <c>Amount</c> is the line's total for its quantity rather than a unit price - the agent states
    /// it that way - so it is carried across as it arrived and never multiplied a second time. Summing is the
    /// application's, because asking the model to add is how a total comes to disagree with its own parts.
    /// </remarks>
    private static WorkspaceSuggestionCandidate CandidateOf(MicrosoftFoundrySuggestionAgentOption option)
    {
        var lines = option.Lines
            .Select(line => new WorkspaceSuggestionCandidateLine(
                line.Slot,
                line.Sku,
                line.Name,
                line.Quantity,
                line.Amount))
            .ToArray();

        return new WorkspaceSuggestionCandidate(lines.Sum(line => line.LineTotal), option.Rationale, lines);
    }
}
