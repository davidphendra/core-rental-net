namespace CoreRentalNet.Evaluation.Reporting.Bias;

/// <summary>Measures verifier refusals for the reasoning stage breakdown.</summary>
public sealed class VerifierBiasBuilder : IBiasSectionBuilder
{
    public BiasSectionName Section => BiasSectionName.Reasoning;

    public IEnumerable<BiasMetric> Build(BiasInput input)
    {
        var decisions = input.Runs
            .GroupBy(run => run.Run.Case.RowNumber)
            .Select(group => group.First())
            .Select(run => TraceResponseReader.VerifierAccepted(run.Run.Trace))
            .Where(accepted => accepted is not null)
            .ToArray();

        if (decisions.Length == 0)
        {
            yield return BiasMetric.NotApplicable(
                Section,
                "Verifier Refusal Rate",
                "No verifier decisions were found in the trace.",
                "verifier");
            yield break;
        }

        var refused = decisions.Count(accepted => accepted == false);
        yield return BiasMetric.Computed(
            Section,
            "Verifier Refusal Rate",
            refused / (double)decisions.Length,
            decisions.Length,
            "verifier");
    }
}
