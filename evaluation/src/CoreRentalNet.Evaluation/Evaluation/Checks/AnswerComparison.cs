using CoreRentalNet.Evaluation.Cases.Model;
using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;
using CoreRentalNet.Evaluation.Evidence.Trace;
using CoreRentalNet.Evaluation.Evidence.Projection;
using CoreRentalNet.Evaluation.Evidence.Context;

using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;
namespace CoreRentalNet.Evaluation.Evaluation.Checks;

/// <summary>Compares approved candidates with the expected setup variants.</summary>
public static class AnswerComparison
{
    public static AnswerResult Evaluate(EvaluationContext context)
    {
        if (context.Candidates.Count == 0)
        {
            return new AnswerResult(false, 0, [FailureCodes.NoCandidate]);
        }

        var exact = context.ExpectedVariants.Any(variant => variant.Matches(context.Candidates));
        var f1 = context.ExpectedVariants.Max(variant => variant.F1(context.Candidates));

        if (exact)
        {
            return new AnswerResult(true, f1, []);
        }

        var variant = context.ExpectedVariants
            .OrderByDescending(variant => variant.F1(context.Candidates))
            .First();
        var candidate = context.Candidates
            .OrderByDescending(variant.F1)
            .First();

        return new AnswerResult(false, f1, Failures(variant, candidate));
    }

    private static IReadOnlyList<string> Failures(
        ExpectedSetupVariant expected,
        Candidate actual)
    {
        var failures = new List<string>();

        var expectedBySlot = expected.Lines.ToDictionary(
            line => line.Slot,
            line => line.Skus.ToHashSet(StringComparer.Ordinal),
            StringComparer.Ordinal);

        var actualBySlot = actual.Lines
            .GroupBy(line => line.Slot, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(line => line.Sku).ToHashSet(StringComparer.Ordinal),
                StringComparer.Ordinal);

        foreach (var (slot, expectedSkus) in expectedBySlot)
        {
            if (!actualBySlot.TryGetValue(slot, out var actualSkus))
            {
                failures.Add(FailureCodes.MissingSlot(slot));
                continue;
            }

            if (!expectedSkus.SetEquals(actualSkus))
            {
                failures.Add(FailureCodes.WrongSku(slot));
            }
        }

        failures.AddRange(actualBySlot.Keys
            .Where(slot => !expectedBySlot.ContainsKey(slot))
            .Select(FailureCodes.ExtraSlot));

        return [.. failures.Distinct(StringComparer.Ordinal)];
    }
}
