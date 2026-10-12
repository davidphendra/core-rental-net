using System.Text.Json.Serialization;

namespace CoreRentalNet.CatalogPopulate.Metadata;

/// <summary>The fields the local model produces for one product.</summary>
internal sealed record GeneratedProductMetadata(
    [property: JsonPropertyName("tags")] IReadOnlyList<string> Tags,
    [property: JsonPropertyName("bestFor")] IReadOnlyList<string> BestFor,
    [property: JsonPropertyName("notFor")] IReadOnlyList<string> NotFor,
    [property: JsonPropertyName("reviewSummary")] string ReviewSummary,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("material")] string Material,
    [property: JsonPropertyName("assemblyRequired")] string AssemblyRequired);
