using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;

namespace CoreRentalNet.Evaluation.Cases.Model;

/// <summary>One row of the test script, with its acceptable expected setups.</summary>
public sealed record EvaluationCase(
    int RowNumber,
    string CaseId,
    string Sentence,
    IReadOnlyList<ExpectedSetupVariant> Variants,
    string? DuplicateGroup,
    string? AmbiguityGroup)
{
    public IReadOnlySet<string> RequestedSlots { get; } = Variants[0].Slots;

    public string ExpectedSummary { get; } = string.Join(
        " | ",
        Variants.Select(variant => variant.Key));
}
