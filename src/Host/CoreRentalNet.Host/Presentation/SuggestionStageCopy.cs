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
    /// <summary>
    /// How many attempts a run may take, as the application states it.
    /// </summary>
    /// <remarks>
    /// Mirrors the agent's bound rather than asking for it, because the number is part of what a
    /// customer is told while waiting and not part of what the agent is asked. A run that says
    /// "attempt 2 of 3" is describing how long this may take; if the agent's own bound changed and this
    /// did not, the words would be wrong and the run would still be right.
    /// </remarks>
    public const int Attempts = 3;

    public static string? For(string? stage)
        => stage is not null && Words.TryGetValue(stage, out var words) ? words : null;

    /// <summary>
    /// Which attempt is running, or nothing for the first - a first attempt is not news.
    /// </summary>
    public static string? Attempt(int attempt)
        => attempt > 1 && attempt <= Attempts
            ? $"attempt {attempt} of {Attempts}"
            : null;
}
