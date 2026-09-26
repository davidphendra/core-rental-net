using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

/// <summary>One line of a candidate: what the agent stated about one SKU, carried to the page.</summary>
/// <remarks>
/// <paramref name="LineTotal"/> is the line's own amount as the agent stated it - already the total for the
/// quantity it carries, and not a unit price. Multiplying it by the quantity again would inflate every
/// candidate.
/// </remarks>
public sealed record WorkspaceSuggestionCandidateLine(
    SlotId Slot,
    string Sku,
    string Name,
    int Quantity,
    decimal LineTotal);
