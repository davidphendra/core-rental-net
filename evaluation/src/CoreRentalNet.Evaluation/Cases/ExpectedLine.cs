namespace CoreRentalNet.Evaluation.Cases;

/// <summary>One expected product in one slot.</summary>
public sealed record ExpectedLine(string Slot, IReadOnlyList<string> Skus);
