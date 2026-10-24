using CoreRentalNet.Evaluation.Evidence;
using CoreRentalNet.Evaluation.Execution;

namespace CoreRentalNet.Evaluation.Reporting.Bias;

/// <summary>One run paired with the context the checks already parsed.</summary>
public sealed record BiasRun(RowRun Run, EvaluationContext Context);
