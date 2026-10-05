using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

namespace CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;

/// <summary>Turns one agent-stated setup into the application's, totalling its lines.</summary>
/// <remarks>
/// A line's <c>amount</c> is already the total for its quantity — the agent states it that way — so it is carried
/// across as it arrived and never multiplied a second time. Summing is the application's, because asking the
/// model to add is how a total comes to disagree with its own parts.
/// </remarks>
internal static class WorkspaceSuggestionCandidateReader
{
    public static WorkspaceSuggestionCandidate From(WorkspaceSuggestionAgentOption option)
    {
        var lines = option.Lines
            .Select(line => new WorkspaceSuggestionCandidateLine(
                line.Slot, line.Sku, line.Name, line.Quantity, line.Amount))
            .ToArray();

        return new WorkspaceSuggestionCandidate(lines.Sum(line => line.LineTotal), lines);
    }
}
