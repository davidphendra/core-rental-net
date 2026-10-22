using System.Text.Json.Serialization;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;

/// <summary>One component's share of a stated total, as the rephraser returns it.</summary>
/// <remarks>
/// An entry is a pair rather than a property on the category so an allocation cannot name a category the answer
/// did not include. The mapper checks that every key is present and that the sum stays within the total.
/// </remarks>
internal sealed record WorkspaceComponentAllocationWire(
    [property: JsonPropertyName("key")]
    [property: JsonRequired]
    WorkspaceComponentCategory ComponentCategory,
    [property: JsonPropertyName("amount")]
    [property: JsonRequired]
    int Amount);
