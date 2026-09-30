namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;

/// <summary>A product the reranker kept, resolved back to the pool's own row.</summary>
/// <remarks>
/// <b>The product is the pool's, not the model's restatement of it.</b> The reranker names a SKU; what the
/// composer reads is the row the tool returned, so a description or a price cannot be improved, shortened or
/// invented between the search and the composition.
/// </remarks>
public sealed record SelectedWorkspaceComponentProduct(
    RetrievedWorkspaceComponentProduct RetrievedProduct,
    ProductRelevanceLevel Relevance,
    string Reason);
