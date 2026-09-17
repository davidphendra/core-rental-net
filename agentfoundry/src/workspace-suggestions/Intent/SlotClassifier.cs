using System.Text.Json;
using AgentFoundry.WorkspaceSuggestions.Contracts;
using AgentFoundry.WorkspaceSuggestions.Prompts;
using AgentFoundry.WorkspaceSuggestions.Vocabularies;
using Microsoft.Extensions.AI;

namespace AgentFoundry.WorkspaceSuggestions.Intent;

/// <summary>
/// Asks a model which parts of a workspace a request means, when the table does not know the phrasing.
/// </summary>
/// <remarks>
/// Only a miss reaches here, and only names that exist leave. The names the application sent are what an
/// answer is checked against - not the vocabulary alone - so a model cannot ask for a part this
/// workspace does not have, and the set the request carried stays the closed list it always was.
/// </remarks>
internal sealed class SlotClassifier(IChatClient client, PromptLibrary prompts) : ISlotClassifier
{
    public async Task<IReadOnlyList<string>> ClassifyAsync(
        string query,
        IReadOnlyList<SlotRule> slots,
        IReadOnlyList<Finding> findings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(slots);
        ArgumentNullException.ThrowIfNull(findings);

        var prompt = prompts.Render("slot-classifier", new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["slots"] = Listed(slots),
            ["query"] = query,
            ["findings"] = Objections(findings),
        });

        using var document = JsonDocument.Parse(
            await PromptAnswer.JsonAsync(client, prompt, cancellationToken).ConfigureAwait(false));

        return Named(document.RootElement, slots);
    }

    /// <summary>The parts that exist, as the prompt states them.</summary>
    private static string Listed(IReadOnlyList<SlotRule> slots)
        => string.Join("\n", slots.Select(rule => $"- {rule.Slot}: \"{rule.DisplayName}\""));

    /// <summary>What was wrong last time, or nothing at all on the first attempt.</summary>
    private static string Objections(IReadOnlyList<Finding> findings)
        => findings.Count == 0
            ? "(nothing yet)"
            : string.Join("\n", findings.Select(finding => $"- {finding.Kind} on {finding.Slot}"));

    /// <summary>
    /// The names the model gave, reduced to the parts that exist.
    /// </summary>
    /// <remarks>
    /// A name outside the vocabulary is dropped rather than trusted, and a repeat is folded away. This is
    /// the same reduction the rephraser performs on whatever it is handed, done here as well because a
    /// classifier that returned an invented part would be handing the workflow something it promised
    /// never to hand it.
    /// </remarks>
    private static IReadOnlyList<string> Named(JsonElement answer, IReadOnlyList<SlotRule> slots)
    {
        if (answer.ValueKind != JsonValueKind.Array)
        {
            throw new PromptAnswerException("The model's answer was not a list of parts.");
        }

        var known = slots.Select(rule => rule.Slot).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return
        [
            .. answer
                .EnumerateArray()
                .Where(name => name.ValueKind == JsonValueKind.String)
                .Select(name => name.GetString())
                .Where(name => name is not null && Slots.IsKnown(name) && known.Contains(name))
                .Select(name => name!)
                .Distinct(StringComparer.OrdinalIgnoreCase),
        ];
    }
}
