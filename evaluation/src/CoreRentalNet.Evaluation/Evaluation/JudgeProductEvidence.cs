namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>One product row shown to the local judge.</summary>
public sealed record JudgeProductEvidence(
    string Slot,
    string Sku,
    string Name,
    string? Description,
    decimal? Amount);
