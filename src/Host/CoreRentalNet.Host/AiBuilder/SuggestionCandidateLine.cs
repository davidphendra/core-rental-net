using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Host.AiBuilder;

/// <summary>One line of a candidate, priced and named by the application rather than by the agent.</summary>
/// <remarks>
/// The agent names a SKU, a quantity and a purpose and nothing else. The name and the amount here come from
/// the catalogue, so a candidate cannot state a price the catalogue does not charge.
/// </remarks>
/// <param name="Slot">Where it goes. Carried so that choosing this candidate can be written as a command
/// without asking the catalogue which slot the product belongs in a second time.</param>
internal sealed record SuggestionCandidateLine(
    SlotId Slot,
    string Sku,
    string Name,
    int Quantity,
    decimal LineTotal);
