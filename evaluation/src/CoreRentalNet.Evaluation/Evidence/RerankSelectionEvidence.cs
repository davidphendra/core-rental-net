namespace CoreRentalNet.Evaluation.Evidence;

/// <summary>One SKU the reranker put in its top selection for a component.</summary>
public sealed record RerankSelectionEvidence(
    string Slot,
    string Sku,
    int Rank,
    int? Attempt);
