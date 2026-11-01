using CoreRentalNet.Evaluation.Evidence.Trace;
using CoreRentalNet.Evaluation.Evidence.Context;

namespace CoreRentalNet.Evaluation.Evidence.Projection;

/// <summary>One SKU the reranker put in its top selection for a component.</summary>
public sealed record RerankSelectionEvidence(
    string Slot,
    string Sku,
    int Rank,
    int? Attempt);
