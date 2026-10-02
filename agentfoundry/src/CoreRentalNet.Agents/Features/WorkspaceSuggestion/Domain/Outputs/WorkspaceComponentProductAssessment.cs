using System.Text.Json.Serialization;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;
/// <summary>One product as the reranker judged it: how relevant to this component, and why.</summary>
/// <remarks>
/// <b>No slot and no rank.</b> The slot is the pool's to state — the reranker is given products, not a workspace
/// — and the order of the list is the rank, so a field for it would be a second place to be wrong. The reason is
/// read by the composer and by nothing else, which is why no model text reaches a caller.
/// </remarks>
public sealed record WorkspaceComponentProductAssessment(
    [property: JsonPropertyName("sku")]
    [property: JsonRequired]
    string Sku,
    [property: JsonPropertyName("relevance")]
    [property: JsonRequired]
    ProductRelevanceLevel Relevance,
    [property: JsonPropertyName("reason")]
    [property: JsonRequired]
    string Reason);
