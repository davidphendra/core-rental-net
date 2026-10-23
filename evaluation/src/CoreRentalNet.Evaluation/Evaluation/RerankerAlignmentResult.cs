namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>Whether the reranker returned a non-empty selection drawn from its input.</summary>
public sealed record RerankerAlignmentResult(
    bool Aligned,
    IReadOnlyList<string> AlignedSlots,
    bool HasUnknownSku);
