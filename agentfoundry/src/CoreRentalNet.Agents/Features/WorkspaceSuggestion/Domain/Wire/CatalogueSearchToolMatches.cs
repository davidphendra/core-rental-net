using System.Text.Json.Serialization;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Wire;

/// <summary>The rows one catalogue answer carried.</summary>
public sealed record CatalogueSearchToolMatches(
    [property: JsonPropertyName("value")]
    IReadOnlyList<CatalogueSearchToolItem> Value);
