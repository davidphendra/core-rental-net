using System.Text.Json.Serialization;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;

/// <summary>One relevant component as the rephraser returns it.</summary>
/// <remarks>
/// Presence in the answer is relevance, so the wire has no <c>relevant</c> flag and no omitted categories. The
/// per-component budget is gone too: a total and an optional allocation list carry the same information with far
/// fewer repeated fields.
/// </remarks>
internal sealed record WorkspaceComponentExpansionWire(
    [property: JsonPropertyName("key")]
    [property: JsonRequired]
    WorkspaceComponentCategory ComponentCategory,
    [property: JsonPropertyName("retrieval_query")]
    [property: JsonRequired]
    string RetrievalQuery,
    [property: JsonPropertyName("search_terms")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<string>? SearchTerms = null,
    [property: JsonPropertyName("synonyms")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<string>? Synonyms = null,
    [property: JsonPropertyName("semantic_concepts")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<string>? SemanticConcepts = null);
