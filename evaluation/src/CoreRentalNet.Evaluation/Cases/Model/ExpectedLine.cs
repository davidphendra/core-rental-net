using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;

namespace CoreRentalNet.Evaluation.Cases.Model;

/// <summary>One expected product in one slot.</summary>
public sealed record ExpectedLine(string Slot, IReadOnlyList<string> Skus);
