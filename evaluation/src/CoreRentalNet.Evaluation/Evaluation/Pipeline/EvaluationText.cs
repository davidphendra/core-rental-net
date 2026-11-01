using CoreRentalNet.Evaluation.Cases.Model;
using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;

using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;
namespace CoreRentalNet.Evaluation.Evaluation.Pipeline;

/// <summary>Renders the human-readable expected and actual summaries.</summary>
public static class EvaluationText
{
    public static string Expected(IReadOnlyList<ExpectedSetupVariant> variants)
        => string.Join(
            " OR ",
            variants.Select(variant => string.Join(
                "; ",
                variant.Lines.Select(line =>
                    $"{line.Slot}: {string.Join(", ", line.Skus)}"))));

    public static string Candidate(IReadOnlyList<Candidate> candidates)
        => string.Join(
            " OR ",
            candidates.Select(candidate => string.Join(
                "; ",
                candidate.Lines.Select(line =>
                    $"{line.Slot}: {line.Name} ({line.Sku})"))));
}
