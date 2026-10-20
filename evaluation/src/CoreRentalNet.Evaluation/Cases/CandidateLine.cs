namespace CoreRentalNet.Evaluation.Cases;

/// <summary>One composed line, as the agent approved it.</summary>
public sealed record CandidateLine(
    string Slot,
    string Sku,
    string Name,
    int Quantity,
    decimal Amount);
