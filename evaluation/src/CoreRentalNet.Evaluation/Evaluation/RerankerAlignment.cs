using CoreRentalNet.Evaluation.Evidence;

namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>Recognises a reranker selection that only uses the SKUs it was given.</summary>
public static class RerankerAlignment
{
    public static RerankerAlignmentResult Evaluate(EvaluationContext context)
    {
        var input = InputSkusBySlot(context.FinalToolAnswers);
        var output = OutputSkusBySlot(context.FinalRerankSelections);
        var alignedSlots = new List<string>();
        var hasUnknownSku = false;

        foreach (var (slot, returned) in output)
        {
            if (returned.Count == 0)
            {
                continue;
            }

            if (!input.TryGetValue(slot, out var given))
            {
                hasUnknownSku = true;
                continue;
            }

            if (returned.Any(sku => !given.Contains(sku)))
            {
                hasUnknownSku = true;
                continue;
            }

            alignedSlots.Add(slot);
        }

        return new RerankerAlignmentResult(
            Aligned: alignedSlots.Count > 0 && !hasUnknownSku,
            AlignedSlots: alignedSlots,
            HasUnknownSku: hasUnknownSku);
    }

    private static Dictionary<string, HashSet<string>> InputSkusBySlot(
        IEnumerable<ToolAnswerEvidence> answers)
    {
        var bySlot = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var answer in answers)
        {
            if (!bySlot.TryGetValue(answer.Slot, out var skus))
            {
                skus = new HashSet<string>(StringComparer.Ordinal);
                bySlot[answer.Slot] = skus;
            }

            foreach (var sku in answer.ReturnedSkus)
            {
                skus.Add(sku);
            }
        }

        return bySlot;
    }

    private static Dictionary<string, List<string>> OutputSkusBySlot(
        IEnumerable<RerankSelectionEvidence> selections)
        => selections
            .GroupBy(selection => selection.Slot, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(selection => selection.Rank)
                    .Select(selection => selection.Sku)
                    .Distinct(StringComparer.Ordinal)
                    .ToList(),
                StringComparer.Ordinal);
}
