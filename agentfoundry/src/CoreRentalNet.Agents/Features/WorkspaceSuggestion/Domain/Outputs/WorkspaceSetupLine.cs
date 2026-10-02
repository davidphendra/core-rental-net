using System.Text.Json.Serialization;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Tools;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Requests;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;
/// <summary>Which SKU goes in which slot, how many, and the composer's own words for why.</summary>
public sealed record WorkspaceSetupLine(
    [property: JsonPropertyName("slot")]
    WorkspaceSlot Slot,
    [property: JsonPropertyName("sku")]
    string Sku,
    [property: JsonPropertyName("name")]
    string Name,
    [property: JsonPropertyName("quantity")]
    int Quantity,
    [property: JsonPropertyName("amount")]
    decimal Amount,
    [property: JsonPropertyName("why")]
    string Why);
