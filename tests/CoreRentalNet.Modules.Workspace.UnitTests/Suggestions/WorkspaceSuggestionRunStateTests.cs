using AwesomeAssertions;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent.Events;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Reading;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;
using CoreRentalNet.Modules.Workspace.Domain;
using Xunit;

namespace CoreRentalNet.Modules.Workspace.UnitTests.Suggestions;

public sealed class WorkspaceSuggestionRunStateTests
{
    [Fact] // a narrative field is emitted once, and only once it has closed
    public void A_narrative_field_is_emitted_exactly_once()
    {
        var state = new WorkspaceSuggestionRunState("a desk");

        state.AppendNarrativeText("{\"rationale\":\"A tidy setup").Should().BeEmpty("a partial value is never shown");

        var closed = state.AppendNarrativeText(" for one person\"}");

        closed.Should().ContainSingle();
        closed[0].Kind.Should().Be(WorkspaceSuggestionNarrativeFieldKind.Rationale);
        closed[0].Text.Should().Be("A tidy setup for one person");

        state.AppendNarrativeText("{\"why\":\"For work\"}").Should().ContainSingle()
            .Which.Kind.Should().Be(WorkspaceSuggestionNarrativeFieldKind.Why);
    }

    [Fact]
    public void A_not_a_workspace_answer_is_refused()
    {
        var state = new WorkspaceSuggestionRunState("a desk");

        var frame = state.CompleteWithAgentAnswer(Ready(WorkspaceSuggestionAnswerStatus.NotWorkspace, []));

        frame.Should().NotBeNull();
        frame!.Status.Should().Be(WorkspaceSuggestionResultFrame.NotWorkspace);
        state.HasEnded.Should().BeTrue();
        state.CreateRunRecord("customer", 0).Verdict.Should().Be(WorkspaceSuggestionVerdict.Refused);
    }

    [Fact] // a suggestion with no candidate is not a suggestion
    public void A_suggestion_with_no_candidate_is_invalid()
    {
        var state = new WorkspaceSuggestionRunState("a desk");

        state.CompleteWithAgentAnswer(Ready(WorkspaceSuggestionAnswerStatus.Suggested, [])).Should().BeNull();
        state.CreateRunRecord("customer", 0).Verdict.Should().Be(WorkspaceSuggestionVerdict.Invalid);
    }

    [Fact]
    public void A_suggestion_with_candidates_is_shown_as_stated()
    {
        var state = new WorkspaceSuggestionRunState("a desk");

        var frame = state.CompleteWithAgentAnswer(Ready(WorkspaceSuggestionAnswerStatus.Suggested, [Candidate()]));

        frame.Should().NotBeNull();
        frame!.Status.Should().Be(WorkspaceSuggestionResultFrame.Suggested);
        frame.Candidates.Should().ContainSingle();
        state.CreateRunRecord("customer", 0).Verdict.Should().Be(WorkspaceSuggestionVerdict.Suggested);
    }

    [Fact]
    public void An_unavailable_agent_ends_the_run()
    {
        var state = new WorkspaceSuggestionRunState("a desk");

        state.MarkUnavailable();

        state.HasEnded.Should().BeTrue();
        state.CreateRunRecord("customer", 0).Verdict.Should().Be(WorkspaceSuggestionVerdict.Unavailable);
    }

    internal static WorkspaceSuggestionResultReadyEvent Ready(
        WorkspaceSuggestionAnswerStatus status,
        IReadOnlyList<WorkspaceSuggestionCandidate> candidates)
        => new(new WorkspaceSuggestionAnswer(status, candidates, null), "hash");

    internal static WorkspaceSuggestionCandidate Candidate()
        => new(100_000m, "A tidy setup.", [new WorkspaceSuggestionCandidateLine(SlotId.Desk, "DSK0001", "Desk", 1, 100_000m)]);
}
