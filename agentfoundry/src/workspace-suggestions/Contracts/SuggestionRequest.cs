namespace AgentFoundry.WorkspaceSuggestions.Contracts;

/// <summary>
/// What the application asks for: one customer's words, and the slot rules the application owns.
/// </summary>
/// <remarks>
/// The rules travel with the request rather than being copied here, because a capacity has one owner -
/// the application's own slot settings - and a second copy is a copy that drifts. Nothing else comes
/// with it: the entitlement is the application's to decide, and everything else the agent needs it
/// reads from the catalogue.
/// </remarks>
public sealed record SuggestionRequest(
    string RequestId,
    string Query,
    IReadOnlyList<SlotRule> Slots);
