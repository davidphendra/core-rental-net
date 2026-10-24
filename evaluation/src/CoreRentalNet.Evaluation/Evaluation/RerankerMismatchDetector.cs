using CoreRentalNet.Evaluation.Evidence;

namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>Describes the expected, raw, selected and composed SKU differences.</summary>
public static class RerankerMismatchDetector
{
    public static RerankerMismatch Detect(EvaluationContext context)
    {
        var expected = context.ExpectedVariants
            .SelectMany(variant => variant.Skus)
            .ToHashSet(StringComparer.Ordinal);
        var raw = context.FinalToolAnswers
            .SelectMany(answer => answer.ReturnedSkus)
            .ToHashSet(StringComparer.Ordinal);
        var selected = context.FinalRerankSelections
            .Select(selection => selection.Sku)
            .ToHashSet(StringComparer.Ordinal);
        var composed = context.Candidates
            .SelectMany(candidate => candidate.Lines)
            .Select(line => line.Sku)
            .ToHashSet(StringComparer.Ordinal);

        var missingFromRaw = expected.Where(sku => !raw.Contains(sku)).Order(StringComparer.Ordinal).ToArray();
        var dropped = expected
            .Where(sku => raw.Contains(sku) && !selected.Contains(sku))
            .Order(StringComparer.Ordinal)
            .ToArray();
        var selectedNotExpected = selected
            .Where(sku => !expected.Contains(sku))
            .Order(StringComparer.Ordinal)
            .ToArray();
        var composerChanged = !composed.SetEquals(selected);

        return new RerankerMismatch(
            missingFromRaw,
            dropped,
            selectedNotExpected,
            composerChanged,
            Summary(missingFromRaw, dropped, selectedNotExpected, composerChanged));
    }

    private static string Summary(
        IReadOnlyList<string> missingFromRaw,
        IReadOnlyList<string> dropped,
        IReadOnlyList<string> selectedNotExpected,
        bool composerChanged)
    {
        var parts = new List<string>();

        if (missingFromRaw.Count > 0)
        {
            parts.Add($"Expected SKUs absent before reranking: {string.Join(", ", missingFromRaw)}.");
        }

        if (dropped.Count > 0)
        {
            parts.Add($"Expected SKUs dropped by the reranker: {string.Join(", ", dropped)}.");
        }

        if (selectedNotExpected.Count > 0)
        {
            parts.Add($"Selected SKUs not expected: {string.Join(", ", selectedNotExpected)}.");
        }

        if (composerChanged)
        {
            parts.Add("The composer changed the reranker selection.");
        }

        return parts.Count == 0 ? "No reranker mismatch." : string.Join(" ", parts);
    }
}
