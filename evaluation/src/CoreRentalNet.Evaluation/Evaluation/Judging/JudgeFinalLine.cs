using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;

namespace CoreRentalNet.Evaluation.Evaluation.Judging;

/// <summary>One final composed line, including the quantity the judge must count.</summary>
public sealed record JudgeFinalLine(
    string Slot,
    string Sku,
    string Name,
    int Quantity,
    decimal Amount,
    string? Description);
