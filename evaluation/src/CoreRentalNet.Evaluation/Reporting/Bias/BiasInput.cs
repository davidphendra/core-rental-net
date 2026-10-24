using CoreRentalNet.Evaluation.Cases;
using CoreRentalNet.Evaluation.Evaluation;

namespace CoreRentalNet.Evaluation.Reporting.Bias;

/// <summary>Everything the bias builders read.</summary>
public sealed record BiasInput(
    IReadOnlyList<CaseResult> Cases,
    IReadOnlyList<BiasRun> Runs,
    IReadOnlyList<MetricRow> Metrics,
    IReadOnlyDictionary<string, ProductRecord> Catalogue);
