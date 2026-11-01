using CoreRentalNet.Evaluation.Evidence.Trace;
using CoreRentalNet.Evaluation.Evidence.Projection;
using CoreRentalNet.Evaluation.Evidence.Context;
using CoreRentalNet.Evaluation.Execution.Transport;
using CoreRentalNet.Evaluation.Execution.Auth;
using CoreRentalNet.Evaluation.Execution.Tools;
using CoreRentalNet.Evaluation.Execution.Running;

namespace CoreRentalNet.Evaluation.Reporting.Bias;

/// <summary>One run paired with the context the checks already parsed.</summary>
public sealed record BiasRun(RowRun Run, EvaluationContext Context);
