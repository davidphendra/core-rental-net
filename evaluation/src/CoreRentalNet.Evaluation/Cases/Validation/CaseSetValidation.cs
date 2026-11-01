using CoreRentalNet.Evaluation.Cases.Model;
using CoreRentalNet.Evaluation.Cases.Parsing;

namespace CoreRentalNet.Evaluation.Cases.Validation;

/// <summary>The result of checking the fixture before any evaluation run.</summary>
public sealed record CaseSetValidation(
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings);
