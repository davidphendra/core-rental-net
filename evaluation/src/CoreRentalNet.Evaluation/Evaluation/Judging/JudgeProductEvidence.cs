using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;

namespace CoreRentalNet.Evaluation.Evaluation.Judging;

/// <summary>One product row shown to the local judge.</summary>
public sealed record JudgeProductEvidence(
    string Slot,
    string Sku,
    string Name,
    string? Description,
    decimal? Amount);
