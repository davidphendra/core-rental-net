using CoreRentalNet.Evaluation.Cases.Model;
using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;
using CoreRentalNet.Evaluation.Execution.Transport;
using CoreRentalNet.Evaluation.Execution.Auth;
using CoreRentalNet.Evaluation.Execution.Tools;
using CoreRentalNet.Evaluation.Execution.Running;

using CoreRentalNet.Evaluation.Evidence.Trace;
using CoreRentalNet.Evaluation.Evidence.Projection;
namespace CoreRentalNet.Evaluation.Evidence.Context;

/// <summary>Builds one evaluation context from a run's events and raw trace.</summary>
public sealed class EvaluationContextFactory(
    WorkspaceTraceProjector projector,
    SlotCapacities capacities)
{
    public EvaluationContext Create(RowRun run)
    {
        var facts = StreamEventFacts.Read(run.Events);
        var evidence = projector.Project(run.Trace);

        return new EvaluationContext(
            ExpectedVariants: run.Case.Variants,
            Capacities: capacities.AsDictionary(),
            Candidates: facts.Candidates,
            Stages: facts.Stages,
            RunStatus: facts.RunStatus,
            FinalAttempt: facts.FinalAttempt,
            CompletedIsLast: facts.CompletedIsLast,
            ToolAnswers: evidence.ToolAnswers,
            RerankSelections: evidence.RerankSelections);
    }
}
