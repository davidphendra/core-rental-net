using CoreRentalNet.Evaluation.Cases.Model;
using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;
using CoreRentalNet.Evaluation.Contracts.Suggestion;
using CoreRentalNet.Evaluation.Contracts.Catalogue;
using CoreRentalNet.Evaluation.Contracts.Reranker;
using CoreRentalNet.Evaluation.Contracts.Trace;
using CoreRentalNet.Evaluation.Evidence.Trace;
using CoreRentalNet.Evaluation.Evidence.Projection;
using CoreRentalNet.Evaluation.Evidence.Context;

using CoreRentalNet.Evaluation.Execution.Transport;
using CoreRentalNet.Evaluation.Execution.Auth;
using CoreRentalNet.Evaluation.Execution.Tools;
namespace CoreRentalNet.Evaluation.Execution.Running;

/// <summary>One execution of one case at one repetition, with the evidence it produced.</summary>
public sealed record RowRun(
    EvaluationCase Case,
    int Repetition,
    string RunId,
    IReadOnlyList<StreamEventDto> Events,
    RunTrace Trace)
{
    /// <summary>True when the case was stopped by its own execution budget.</summary>
    public bool TimedOut { get; init; }

    /// <summary>True when the agent response stream reached its terminal event.</summary>
    public bool Completed { get; init; }

    /// <summary>The actual case wall time measured by the runner.</summary>
    public double WallMs { get; init; }
}
