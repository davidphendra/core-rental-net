namespace CoreRentalNet.Host.AiBuilder;

/// <summary>One line of a candidate, priced and named by the application rather than by the agent.</summary>
/// <remarks>
/// The agent names a SKU, a quantity and a purpose and nothing else. The name and the amount here come from
/// the catalogue, so a candidate cannot state a price the catalogue does not charge.
/// </remarks>
internal sealed record SuggestionCandidateLine(
    string Sku,
    string Name,
    int Quantity,
    decimal LineTotal);
