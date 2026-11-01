using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;

namespace CoreRentalNet.Evaluation.Cases.Model;

/// <summary>One composed line, as the agent approved it.</summary>
public sealed record CandidateLine(
    string Slot,
    string Sku,
    string Name,
    int Quantity,
    decimal Amount);
