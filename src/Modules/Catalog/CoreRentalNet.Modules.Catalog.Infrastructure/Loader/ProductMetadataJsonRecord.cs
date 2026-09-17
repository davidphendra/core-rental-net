using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoreRentalNet.Modules.Catalog.Infrastructure.Loader;

/// <summary>Shape of the <c>metadata</c> object in one products.json row. Infrastructure-only.</summary>
/// <remarks>
/// Attributes are read as <see cref="JsonElement"/> rather than as strings so a value that is not a
/// string is refused by name instead of being coerced, and <c>JsonExtensionData</c> is what makes an
/// unknown key visible - a typed record would otherwise ignore one silently, which is how a typo in
/// the file becomes a criterion that never matches.
/// </remarks>
internal class ProductMetadataJsonRecord
{
    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("attributes")]
    public Dictionary<string, JsonElement>? Attributes { get; set; }

    [JsonPropertyName("bestFor")]
    public List<string>? BestFor { get; set; }

    [JsonPropertyName("notFor")]
    public List<string>? NotFor { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Unknown { get; set; }
}
