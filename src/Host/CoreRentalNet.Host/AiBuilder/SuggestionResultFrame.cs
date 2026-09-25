using CoreRentalNet.Host.Agents;

namespace CoreRentalNet.Host.AiBuilder;

/// <summary>What the section is given when a run ends: candidates, or the typed not-a-workspace verdict.</summary>
/// <remarks>
/// <para>
/// The frame the browser reads: the one shape a run's answer takes on the page. The names and the amounts are
/// the agent's, stated from the catalogue tool's own answers; the frame is the application's, because one shape
/// for the page is the application's to fix whatever the model said.
/// </para>
/// <para>
/// A refusal is a <b>result</b> rather than a failure, so it travels here with no candidates rather than as an
/// error - the application words it, and there is nothing for a customer to retry.
/// </para>
/// <para>
/// <b>The answer becomes a frame here rather than in the run that reads it</b>, because this type is the shape
/// the answer is being translated into: a run that had to know both shapes to hand one to the other would be
/// the place the two drift apart.
/// </para>
/// </remarks>
internal sealed record SuggestionResultFrame(string Status, IReadOnlyList<SuggestionCandidate> Candidates)
{
    /// <summary>The agent proposed candidates, and they are shown as it stated them.</summary>
    public const string Suggested = "suggested";

    /// <summary>The typed verdict that the request was not about a workspace.</summary>
    public const string NotWorkspace = "notWorkspace";

    public static SuggestionResultFrame Refused()
        => new(NotWorkspace, []);

    public static SuggestionResultFrame Of(IReadOnlyList<SuggestionCandidate> candidates)
        => new(Suggested, candidates);

    /// <summary>The frame an agent's answer becomes, or <c>null</c> when there is nothing in it to show.</summary>
    /// <remarks>
    /// <b>One rule survives here, and it is not a validation.</b> An answer that says it suggested something and
    /// carries no option at all is not a suggestion: there is nothing to present, and an empty candidate list is
    /// not a result worth sending. Everything else crosses as the agent stated it, and what makes a candidate
    /// true is the workspace refusing to apply one it cannot honour.
    /// </remarks>
    public static SuggestionResultFrame? From(AgentSuggestionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.Status is AgentSuggestionStatus.NotWorkspace)
        {
            return Refused();
        }

        return result.Options.Count == 0 ? null : Of([.. result.Options.Select(CandidateOf)]);
    }

    /// <summary>One candidate, totalled from the lines the agent stated.</summary>
    /// <remarks>
    /// A line's <c>Amount</c> is the line's total for its quantity rather than a unit price - the agent states it
    /// that way - so it is carried across as it arrived and never multiplied a second time. Summing is the
    /// application's, because asking the model to add is how a total comes to disagree with its own parts.
    /// </remarks>
    private static SuggestionCandidate CandidateOf(AgentSuggestionOption option)
    {
        var lines = option.Lines
            .Select(line => new SuggestionCandidateLine(
                line.Slot,
                line.Sku,
                line.Name,
                line.Quantity,
                line.Amount))
            .ToArray();

        return new SuggestionCandidate(lines.Sum(line => line.LineTotal), option.Rationale, lines);
    }
}
