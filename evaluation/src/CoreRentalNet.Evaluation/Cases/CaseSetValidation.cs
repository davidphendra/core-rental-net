namespace CoreRentalNet.Evaluation.Cases;

/// <summary>The result of checking the fixture before any evaluation run.</summary>
public sealed record CaseSetValidation(
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings);
