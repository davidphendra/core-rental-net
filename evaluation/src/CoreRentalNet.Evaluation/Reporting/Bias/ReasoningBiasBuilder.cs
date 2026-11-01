using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;

namespace CoreRentalNet.Evaluation.Reporting.Bias;

/// <summary>Calculates decision, grounding and reviewer metrics.</summary>
public sealed class ReasoningBiasBuilder : IBiasSectionBuilder
{
    private const BiasSectionName SectionName = BiasSectionName.Reasoning;

    public BiasSectionName Section => SectionName;
    public IEnumerable<BiasMetric> Build(BiasInput input)
    {
        var runs = input.Runs
            .GroupBy(run => run.Run.Case.RowNumber)
            .Select(group => group.First())
            .ToArray();
        yield return DecisionConsistency(input.Cases);
        yield return BiasMetric.NotApplicable(
            SectionName,
            "Counterfactual Flip Rate",
            "No counterfactual pairs exist in the fixture.");
        yield return UnsupportedAssumptions(runs);
        yield return EvidenceGrounding(runs);
        yield return ToolSelectionDisparity(runs);
        yield return ConstraintViolations(input.Metrics, runs.Length);
    }

    private static BiasMetric DecisionConsistency(IReadOnlyList<CaseResult> cases)
    {
        var groups = cases
            .GroupBy(testCase => testCase.Sentence, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .ToArray();

        if (groups.Length == 0)
        {
            return BiasMetric.Insufficient(
                SectionName,
                "Decision Consistency Rate",
                "No duplicate sentence groups exist in the selected scope.",
                0);
        }

        var consistent = groups.Count(group => group.Select(testCase => testCase.CasePass).Distinct().Count() == 1);
        return BiasMetric.Computed(
            SectionName,
            "Decision Consistency Rate",
            consistent / (double)groups.Length,
            groups.Length);
    }

    private static BiasMetric UnsupportedAssumptions(IReadOnlyList<BiasRun> runs)
    {
        var observed = runs
            .Select(run => (
                Run: run,
                Slots: TraceResponseReader.RephraserSlots(run.Run.Trace)))
            .Where(item => item.Slots is not null)
            .ToArray();

        if (observed.Length == 0)
        {
            return BiasMetric.NotApplicable(
                SectionName,
                "Unsupported Assumption Rate",
                "No rephraser decisions were found in the trace.",
                "rephraser");
        }

        var unsupported = observed.Count(item =>
            !item.Slots!.SetEquals(item.Run.Run.Case.RequestedSlots));
        return BiasMetric.Computed(
            SectionName,
            "Unsupported Assumption Rate",
            unsupported / (double)observed.Length,
            observed.Length,
            "rephraser");
    }

    private static BiasMetric EvidenceGrounding(IReadOnlyList<BiasRun> runs)
    {
        var selected = runs
            .SelectMany(run => run.Context.FinalRerankSelections)
            .Select(selection => selection.Sku)
            .ToArray();
        var raw = runs
            .SelectMany(run => run.Context.FinalToolAnswers)
            .SelectMany(answer => answer.ReturnedSkus)
            .ToHashSet(StringComparer.Ordinal);

        return selected.Length == 0
            ? BiasMetric.NotApplicable(
                SectionName,
                "Evidence Grounding Rate",
                "No reranker selections were found.",
                "builder")
            : BiasMetric.Computed(
                SectionName,
                "Evidence Grounding Rate",
                selected.Count(raw.Contains) / (double)selected.Length,
                selected.Length,
                "builder");
    }

    private static BiasMetric ToolSelectionDisparity(IReadOnlyList<BiasRun> runs)
    {
        var byShape = runs
            .GroupBy(
                run => string.Join("+", run.Run.Case.RequestedSlots.Order(StringComparer.Ordinal)),
                StringComparer.Ordinal)
            .Select(group => group
                .SelectMany(run => run.Context.FinalToolAnswers.Select(answer => answer.ToolName))
                .Distinct(StringComparer.Ordinal)
                .Count())
            .ToArray();

        var difference = BiasStatistics.Difference(byShape.Select(count => (double)count).ToArray());
        return difference is null
            ? BiasMetric.Insufficient(
                SectionName,
                "Tool Selection Disparity",
                "Fewer than two request shapes were observed.",
                byShape.Length,
                "retriever")
            : BiasMetric.Computed(
                SectionName,
                "Tool Selection Disparity",
                difference.Value,
                byShape.Length,
                "retriever");
    }

    private static BiasMetric ConstraintViolations(
        IReadOnlyList<MetricRow> metrics,
        int caseCount)
    {
        var rows = metrics
            .Where(metric => string.Equals(metric.Metric, MetricNames.ConstraintCompliance, StringComparison.Ordinal))
            .ToArray();

        return rows.Length == 0
            ? BiasMetric.Insufficient(SectionName, "Constraint Violation Rate", "No constraint metrics.", caseCount, "builder")
            : BiasMetric.Computed(
                SectionName,
                "Constraint Violation Rate",
                rows.Count(row => !row.Passed) / (double)rows.Length,
                rows.Length,
                "builder");
    }
}
