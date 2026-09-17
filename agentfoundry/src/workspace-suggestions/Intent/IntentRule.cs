namespace AgentFoundry.WorkspaceSuggestions.Intent;

/// <summary>One validated row of the intent table: the phrasings that mean a set, and the set.</summary>
/// <remarks>
/// Internal to the table: what a caller asks is "what does this request mean", not "which row matched".
/// Every phrasing is kept, because a row is a set of ways of saying one thing and matching only the
/// first would make the rest decoration.
/// </remarks>
internal sealed record IntentRule(IReadOnlyList<string> Phrases, IReadOnlyList<string> Slots);
