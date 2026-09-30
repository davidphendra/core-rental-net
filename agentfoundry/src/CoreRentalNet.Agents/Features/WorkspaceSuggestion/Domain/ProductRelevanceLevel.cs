using System.Text.Json.Serialization;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;

/// <summary>How directly a product answers the need of the component it was retrieved for.</summary>
/// <remarks>
/// <b>The converter is not decoration.</b> Without it the enum serializes as its ordinal, and <c>0</c> is not one
/// of the three words the contract names — the trap this repository has already been caught by once, where a
/// global camel-case converter overrode an enum's own wire words.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<ProductRelevanceLevel>))]
public enum ProductRelevanceLevel
{
    [JsonStringEnumMemberName("high")]
    High,

    [JsonStringEnumMemberName("medium")]
    Medium,

    [JsonStringEnumMemberName("low")]
    Low,
}
