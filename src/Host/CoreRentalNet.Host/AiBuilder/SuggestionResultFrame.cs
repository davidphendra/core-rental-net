namespace CoreRentalNet.Host.AiBuilder;

/// <summary>What the section is given when a run ends: candidates, or the typed not-a-workspace verdict.</summary>
/// <remarks>
/// <para>
/// The frame the browser reads, and the point at which an agent's answer has become an application fact: the
/// names, the amounts and the labels are the application's, and the candidates here have been checked before
/// anything was rendered from them.
/// </para>
/// <para>
/// A refusal is a <b>result</b> rather than a failure, so it travels here with no candidates rather than as an
/// error - the application words it, and there is nothing for a customer to retry.
/// </para>
/// </remarks>
internal sealed record SuggestionResultFrame(string Status, IReadOnlyList<SuggestionCandidate> Candidates)
{
    /// <summary>The agent proposed candidates, and every one of them was checked.</summary>
    public const string Suggested = "suggested";

    /// <summary>The typed verdict that the request was not about a workspace.</summary>
    public const string NotWorkspace = "notWorkspace";

    public static SuggestionResultFrame Refused()
        => new(NotWorkspace, []);

    public static SuggestionResultFrame Of(IReadOnlyList<SuggestionCandidate> candidates)
        => new(Suggested, candidates);
}
