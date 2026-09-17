namespace CoreRentalNet.Host.Presentation;

/// <summary>
/// What the application says while a suggestion run is happening, keyed by the agent's stage ids.
/// </summary>
/// <remarks>
/// <para>
/// The agent sends ids and this owns the words, which is what stops a stage renamed in the agent from
/// changing what a customer reads. An id this map does not know renders **nothing** rather than the raw
/// id: a page that leaked <c>selecting</c> into a sentence would be showing an internal name, and one
/// that said "unknown stage" would be showing a defect.
/// </para>
/// <para>
/// The cost of that is drift: a stage added to the agent is a stage this map does not describe, and the
/// run would look as though it had skipped a step. `SuggestionStageCopyTests` asserts every id the agent
/// publishes today, so a new one is caught here rather than by a customer noticing a missing line.
/// </para>
/// </remarks>
internal static class SuggestionStageCopy
{
    private static readonly IReadOnlyDictionary<string, string> Words =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["verifying"] = "Checking your request",
            ["rephrasing"] = "Understanding what you need",
            ["selecting"] = "Choosing products",
            ["reviewing"] = "Reviewing the options",
        };

    /// <summary>The sentence for a stage, or null when this application does not know the id.</summary>
    public static string? For(string? stage)
        => stage is not null && Words.TryGetValue(stage, out var words) ? words : null;
}
