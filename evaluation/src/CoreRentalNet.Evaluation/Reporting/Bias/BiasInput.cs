using CoreRentalNet.Evaluation.Cases.Model;
using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;
using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;

namespace CoreRentalNet.Evaluation.Reporting.Bias;

/// <summary>Everything the bias builders read.</summary>
public sealed record BiasInput(
    IReadOnlyList<CaseResult> Cases,
    IReadOnlyList<BiasRun> Runs,
    IReadOnlyList<MetricRow> Metrics,
    IReadOnlyDictionary<string, ProductRecord> Catalogue);
