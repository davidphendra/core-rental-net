using CoreRentalNet.Evaluation.Cases;
using CoreRentalNet.Evaluation.Contracts;

namespace CoreRentalNet.Evaluation.Evidence;

/// <summary>The workflow facts one run's event stream carries.</summary>
public sealed record StreamEventFacts(
    IReadOnlyList<Candidate> Candidates,
    IReadOnlyList<string> Stages,
    string RunStatus,
    int FinalAttempt,
    bool CompletedIsLast)
{
    public static StreamEventFacts Read(IReadOnlyList<StreamEventDto> events)
    {
        var candidates = events
            .Where(@event => string.Equals(@event.Type, "candidate", StringComparison.Ordinal)
                             && @event.ApprovedWorkspaceSetup is not null)
            .Select(@event => ToCandidate(@event.ApprovedWorkspaceSetup!))
            .ToArray();

        var stages = events
            .Where(@event => string.Equals(@event.Type, "stageStarted", StringComparison.Ordinal)
                             && @event.ProcessingStage is not null)
            .Select(@event => @event.ProcessingStage!)
            .ToArray();

        var completed = events.LastOrDefault(@event =>
            string.Equals(@event.Type, "completed", StringComparison.Ordinal));

        return new StreamEventFacts(
            Candidates: candidates,
            Stages: stages,
            RunStatus: completed?.RunStatus ?? "missing",
            FinalAttempt: completed?.CompletedAttemptCount ?? 1,
            CompletedIsLast: events.Count > 0
                             && string.Equals(events[^1].Type, "completed", StringComparison.Ordinal));
    }

    private static Candidate ToCandidate(CandidateDto candidate)
        => new([.. candidate.Lines.Select(line => new CandidateLine(
            EvaluationSlot.Canonicalize(line.Slot),
            line.Sku,
            line.Name,
            line.Quantity,
            line.Amount))]);
}
