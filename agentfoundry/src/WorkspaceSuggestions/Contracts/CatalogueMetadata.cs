using System.ComponentModel;
using System.Text.Json.Serialization;

namespace WorkspaceSuggestions.Contracts;

/// <summary>What a product says about itself: the vocabulary a request is matched against.</summary>
public sealed record CatalogueMetadata(
    [property: JsonPropertyName("tags")]
    [property: Description("What the product is, in single lower-case words.")]
    IReadOnlyList<string> Tags,
    [property: JsonPropertyName("attributes")]
    [property: Description("Typed facts about the product, every value a string.")]
    IReadOnlyDictionary<string, string> Attributes,
    [property: JsonPropertyName("bestFor")]
    [property: Description("What the product suits.")]
    IReadOnlyList<string> BestFor,
    [property: JsonPropertyName("notFor")]
    [property: Description("What the product does not suit.")]
    IReadOnlyList<string> NotFor);
