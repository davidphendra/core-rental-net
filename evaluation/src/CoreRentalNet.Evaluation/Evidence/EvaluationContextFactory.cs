using CoreRentalNet.Evaluation.Cases;
using CoreRentalNet.Evaluation.Execution;

namespace CoreRentalNet.Evaluation.Evidence;

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
