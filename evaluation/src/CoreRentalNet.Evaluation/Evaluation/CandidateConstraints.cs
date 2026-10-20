using CoreRentalNet.Evaluation.Cases;
using CoreRentalNet.Evaluation.Evidence;

namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>The deterministic workspace constraints, independent of the model judge.</summary>
public static class CandidateConstraints
{
    public static ConstraintResult Evaluate(
        EvaluationContext context,
        IReadOnlyDictionary<string, ProductRecord> catalogue)
    {
        if (context.Candidates.Count == 0)
        {
            return new ConstraintResult(false, [FailureCodes.NoCandidate]);
        }

        var failures = new List<string>();
        var expectedSlots = context.ExpectedVariants[0].Slots;
        var retrievedSkus = context.FinalToolAnswers
            .SelectMany(answer => answer.ReturnedSkus)
            .ToHashSet(StringComparer.Ordinal);

        var candidateKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var candidate in context.Candidates)
        {
            var actualBySlot = candidate.Lines
                .GroupBy(line => line.Slot, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

            failures.AddRange(expectedSlots
                .Where(slot => !actualBySlot.ContainsKey(slot))
                .Select(FailureCodes.ConstraintMissingSlot));

            failures.AddRange(actualBySlot.Keys
                .Where(slot => !expectedSlots.Contains(slot))
                .Select(FailureCodes.ConstraintExtraSlot));

            foreach (var (slot, lines) in actualBySlot)
            {
                if (lines.Select(line => line.Sku).Distinct(StringComparer.Ordinal).Count() != lines.Length)
                {
                    failures.Add(FailureCodes.ConstraintDuplicateLine(slot));
                }

                var capacity = context.Capacities[slot];

                foreach (var line in lines)
                {
                    if (line.Quantity < 1 || line.Quantity > capacity)
                    {
                        failures.Add(FailureCodes.ConstraintCapacity(slot));
                    }

                    if (line.Amount < 0)
                    {
                        failures.Add(FailureCodes.ConstraintNegativeAmount(slot));
                    }

                    if (!catalogue.ContainsKey(line.Sku))
                    {
                        failures.Add(FailureCodes.ConstraintUnknownSku(line.Sku));
                    }

                    if (!retrievedSkus.Contains(line.Sku))
                    {
                        failures.Add(FailureCodes.ConstraintNotRetrieved(line.Sku));
                    }
                }
            }

            var candidateKey = CandidateKey(candidate);
            if (!candidateKeys.Add(candidateKey))
            {
                failures.Add(FailureCodes.ConstraintDuplicateCandidate);
            }
        }

        var distinct = failures.Distinct(StringComparer.Ordinal).ToArray();

        return new ConstraintResult(distinct.Length == 0, distinct);
    }

    private static string CandidateKey(Candidate candidate)
        => string.Join(
            ';',
            candidate.Lines
                .OrderBy(line => line.Slot, StringComparer.Ordinal)
                .ThenBy(line => line.Sku, StringComparer.Ordinal)
                .Select(line => $"{line.Slot}:{line.Sku}:{line.Quantity}"));
}
