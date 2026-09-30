using System.Text.Json.Serialization;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;

/// <summary>One component category, expanded: whether it was asked for, the words that find it, and what it may
/// cost.</summary>
/// <remarks>
/// The words are offered as three lists rather than one because two different searches read them: the terms and
/// synonyms are matched against a product's name, and the semantic concepts and the retrieval query are matched
/// against a product's meaning. A single list would leave the retriever to guess which search each word was
/// written for.
/// </remarks>
public sealed record WorkspaceComponentExpansion(
    [property: JsonPropertyName("relevant")]
    [property: JsonRequired]
    bool IsRelevant,
    [property: JsonPropertyName("retrieval_query")]
    [property: JsonRequired]
    string RetrievalQuery,
    [property: JsonPropertyName("search_terms")]
    [property: JsonRequired]
    IReadOnlyList<string> SearchTerms,
    [property: JsonPropertyName("synonyms")]
    [property: JsonRequired]
    IReadOnlyList<string> Synonyms,
    [property: JsonPropertyName("semantic_concepts")]
    [property: JsonRequired]
    IReadOnlyList<string> SemanticConcepts,
    [property: JsonPropertyName("budget")]
    [property: JsonRequired]
    WorkspaceComponentMonthlyBudget MonthlyBudget);
