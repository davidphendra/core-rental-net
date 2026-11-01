using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;

namespace CoreRentalNet.Evaluation.Cases.Model;

/// <summary>Every test case in the script, one per row.</summary>
public sealed record EvaluationCaseSet(IReadOnlyList<EvaluationCase> Cases)
{
    public int UniqueSentenceCount { get; } = Cases
        .Select(testCase => testCase.Sentence)
        .Distinct(StringComparer.Ordinal)
        .Count();

    public int UniqueSetupCount { get; } = Cases
        .SelectMany(testCase => testCase.Variants)
        .Select(variant => variant.Key)
        .Distinct(StringComparer.Ordinal)
        .Count();
}
