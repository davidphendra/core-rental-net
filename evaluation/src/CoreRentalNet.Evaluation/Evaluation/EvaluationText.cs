using CoreRentalNet.Evaluation.Cases;

namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>Renders the human-readable expected and actual summaries used by Foundry similarity.</summary>
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
