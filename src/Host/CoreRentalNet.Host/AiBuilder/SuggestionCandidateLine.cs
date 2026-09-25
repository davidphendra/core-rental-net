using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Host.AiBuilder;

/// <summary>One line of a candidate: what the agent stated about one SKU, carried to the page.</summary>
/// <remarks>
/// The name and the amount travel as the agent stated them, which is as its catalogue tool returned them. They
/// are not re-read here: the workspace is what refuses a line that cannot be honoured.
/// </remarks>
/// <param name="Slot">Where it goes. Carried so that choosing this candidate can be written as a command
/// without asking the catalogue which slot the product belongs in a second time.</param>
/// <param name="LineTotal">The line's own amount as the agent stated it - already the total for the quantity it
/// carries, and not a unit price. Multiplying it by the quantity again would inflate every candidate.</param>
internal sealed record SuggestionCandidateLine(
    SlotId Slot,
    string Sku,
    string Name,
    int Quantity,
    decimal LineTotal);
