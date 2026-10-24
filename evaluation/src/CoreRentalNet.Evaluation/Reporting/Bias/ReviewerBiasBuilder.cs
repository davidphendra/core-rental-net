using CoreRentalNet.Evaluation.Evaluation;

namespace CoreRentalNet.Evaluation.Reporting.Bias;

/// <summary>Calculates the reviewer disagreement metric for the reasoning section.</summary>
public sealed class ReviewerBiasBuilder : IBiasSectionBuilder
{
    public BiasSectionName Section => BiasSectionName.Reasoning;

    public IEnumerable<BiasMetric> Build(BiasInput input)
    {
        var cases = input.Cases.ToDictionary(testCase => testCase.RowNumber);
        var decisions = input.Runs
            .GroupBy(run => run.Run.Case.RowNumber)
            .Select(group => group.First())
            .Select(run => (
                Run: run,
                Accepted: TraceResponseReader.ReviewerAccepted(run.Run.Trace)))
            .Where(item => item.Accepted is not null
                           && cases.ContainsKey(item.Run.Run.Case.RowNumber))
            .ToArray();

        if (decisions.Length == 0)
        {
            yield return BiasMetric.NotApplicable(
                Section,
                "Reviewer Disagreement Rate",
                "No reviewer decisions were found in the trace.",
                "reviewer");
            yield break;
        }

        var disagreements = decisions.Count(item =>
            item.Accepted!.Value != cases[item.Run.Run.Case.RowNumber].CasePass);
        yield return BiasMetric.Computed(
            Section,
            "Reviewer Disagreement Rate",
            disagreements / (double)decisions.Length,
            decisions.Length,
            "reviewer");
    }
}
