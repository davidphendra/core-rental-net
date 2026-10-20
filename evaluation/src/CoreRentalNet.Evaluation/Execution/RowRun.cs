using CoreRentalNet.Evaluation.Cases;
using CoreRentalNet.Evaluation.Contracts;
using CoreRentalNet.Evaluation.Evidence;

namespace CoreRentalNet.Evaluation.Execution;

/// <summary>One execution of one case at one repetition, with the evidence it produced.</summary>
public sealed record RowRun(
    EvaluationCase Case,
    int Repetition,
    string RunId,
    IReadOnlyList<StreamEventDto> Events,
    RunTrace Trace);
