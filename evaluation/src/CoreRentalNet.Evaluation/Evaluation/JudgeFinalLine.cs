namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>One final composed line, including the quantity the judge must count.</summary>
public sealed record JudgeFinalLine(
    string Slot,
    string Sku,
    string Name,
    int Quantity,
    decimal Amount,
    string? Description);
