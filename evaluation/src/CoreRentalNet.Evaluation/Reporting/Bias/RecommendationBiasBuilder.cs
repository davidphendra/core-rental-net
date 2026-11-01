using CoreRentalNet.Evaluation.Cases.Model;
using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;

namespace CoreRentalNet.Evaluation.Reporting.Bias;

/// <summary>Calculates catalogue and retrieval bias metrics that do not require group labels.</summary>
public sealed class RecommendationBiasBuilder : IBiasSectionBuilder
{
    private const string MissingGroups = "No protected-group labels exist in the fixture.";
    private const BiasSectionName SectionName = BiasSectionName.Recommendation;

    public BiasSectionName Section => SectionName;

    public IEnumerable<BiasMetric> Build(BiasInput input)
    {
        var runs = input.Runs
            .GroupBy(run => run.Run.Case.RowNumber)
            .Select(group => group.First())
            .ToArray();

        yield return BiasMetric.NotApplicable(SectionName, "Group Exposure Difference", MissingGroups);
        yield return BiasMetric.NotApplicable(SectionName, "Recall@K by Group", MissingGroups);
        yield return BiasMetric.NotApplicable(SectionName, "NDCG@K by Group", MissingGroups);

        yield return Diversity(runs);
        yield return Exposure(runs);
        yield return Coverage(runs);

        yield return BiasMetric.NotApplicable(
            SectionName,
            "Counterfactual Consistency",
            "No counterfactual pairs exist in the fixture.");
        yield return BiasMetric.NotApplicable(
            SectionName,
            "Attribute Sensitivity",
            "No controlled attribute pairs exist in the fixture.");
    }

    private static BiasMetric Diversity(IReadOnlyList<BiasRun> runs)
    {
        var lines = runs
            .SelectMany(run => run.Context.Candidates)
            .SelectMany(candidate => candidate.Lines)
            .ToArray();

        return lines.Length == 0
            ? BiasMetric.Insufficient(SectionName, "Recommendation Diversity", "No composed lines.", 0)
            : BiasMetric.Computed(
                SectionName,
                "Recommendation Diversity",
                lines.Select(line => line.Sku).Distinct(StringComparer.Ordinal).Count() / (double)lines.Length,
                lines.Length);
    }

    private static BiasMetric Exposure(IReadOnlyList<BiasRun> runs)
    {
        var expected = new Dictionary<string, int>(StringComparer.Ordinal);
        var actual = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var run in runs)
        {
            foreach (var slot in run.Run.Case.RequestedSlots)
            {
                expected[slot] = expected.GetValueOrDefault(slot) + 1;
            }

            foreach (var line in run.Context.Candidates.SelectMany(candidate => candidate.Lines))
            {
                actual[line.Slot] = actual.GetValueOrDefault(line.Slot) + 1;
            }
        }

        var difference = BiasStatistics.ExposureDifference(expected, actual);

        return difference is null
            ? BiasMetric.Insufficient(SectionName, "Category Exposure", "No comparable category exposure.", 0)
            : BiasMetric.Computed(SectionName, "Category Exposure", difference.Value, actual.Values.Sum());
    }

    private static BiasMetric Coverage(IReadOnlyList<BiasRun> runs)
    {
        var expected = runs
            .SelectMany(run => run.Context.ExpectedVariants)
            .SelectMany(variant => variant.Skus)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var raw = runs
            .SelectMany(run => run.Context.FinalToolAnswers)
            .SelectMany(answer => answer.ReturnedSkus)
            .ToHashSet(StringComparer.Ordinal);
        var found = expected.Count(raw.Contains);

        return expected.Length == 0
            ? BiasMetric.Insufficient(SectionName, "Retrieval Coverage", "No expected SKUs.", 0)
            : BiasMetric.Computed(SectionName, "Retrieval Coverage", found / (double)expected.Length, expected.Length);
    }
}
