using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

namespace CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;

/// <summary>Reads the provider's streamed answer into the application's own answer, and nothing else.</summary>
/// <remarks>
/// <b>The provider's wire shape stops here.</b> Everything downstream of this type is the application's
/// vocabulary, so the adapter and the run never name a provider record. The answer and the run's cost are each
/// found by what they carry rather than where they sit.
/// </remarks>
internal static class MicrosoftFoundrySuggestionAnswerReader
{
    /// <summary>The typed answer and the run's cost, or <c>null</c> when there is no answer in the text.</summary>
    public static WorkspaceSuggestionAnswer? ReadAnswer(string text)
    {
        var objects = MicrosoftFoundrySuggestionAgentResponseObjects.All(text);

        var response = objects
            .Where(element => MicrosoftFoundrySuggestionAgentResponseObjects.Has(element, "status"))
            .Select(MicrosoftFoundrySuggestionAgentResponseObjects.Read<MicrosoftFoundrySuggestionAgentResponse>)
            .LastOrDefault(candidate => candidate is not null);

        if (response is null)
        {
            return null;
        }

        var runUsage = objects
            .Where(element => MicrosoftFoundrySuggestionAgentResponseObjects.Has(element, "runUsage"))
            .Select(MicrosoftFoundrySuggestionAgentResponseObjects.Read<MicrosoftFoundrySuggestionAgentRunUsageReport>)
            .LastOrDefault(report => report?.RunUsage is not null)
            ?.RunUsage;

        return ToAnswer(response, runUsage);
    }

    /// <summary>The provider's answer, in the application's own words, before the port is crossed.</summary>
    private static WorkspaceSuggestionAnswer ToAnswer(
        MicrosoftFoundrySuggestionAgentResponse response,
        MicrosoftFoundrySuggestionAgentRunUsage? runUsage)
        => new(
            Status: response.Status switch
            {
                MicrosoftFoundrySuggestionAgentStatus.NotWorkspace => WorkspaceSuggestionAnswerStatus.NotWorkspace,
                MicrosoftFoundrySuggestionAgentStatus.CatalogueUnavailable => WorkspaceSuggestionAnswerStatus.CatalogueUnavailable,
                _ => WorkspaceSuggestionAnswerStatus.Suggested,
            },
            Candidates: [.. response.Options.Select(CandidateOf)],
            RunUsage: runUsage is null ? null : new WorkspaceSuggestionRunUsage(
                runUsage.ModelCalls,
                runUsage.InputTokens,
                runUsage.OutputTokens,
                runUsage.Model,
                runUsage.PromptVersion));

    /// <summary>One candidate, totalled from the lines the provider stated.</summary>
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
