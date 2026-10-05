using AwesomeAssertions;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent.Events;
using CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;
using CoreRentalNet.Modules.Workspace.Domain;
using Xunit;

namespace CoreRentalNet.IntegrationTests.Suggestions;

/// <summary>
/// The reader that surfaces a run's progress while the run is still going.
/// </summary>
/// <remarks>
/// The regression this file exists for: the stages used to be read out of the whole text only once the stream had
/// ended, so every stage frame reached the panel at the same moment as the result and the panel showed nothing
/// while the run ran. Each test feeds one fragment at a time, because the whole point is that a closed event is
/// reported before the ending exists.
/// </remarks>
public sealed class WorkspaceSuggestionProgressReaderTests
{
    private const string StageStarted =
        """{"type":"stageStarted","customerWorkflowIdentifier":"run-1","processingStage":"verifyingRequest"}""";

    [Fact]
    public void A_stage_is_reported_when_its_object_closes_and_not_at_the_end()
    {
        var progress = new WorkspaceSuggestionProgressReader().Read(StageStarted);

        progress.Should().ContainSingle();
        progress[0].Should().BeOfType<WorkspaceSuggestionStageStartedEvent>()
            .Which.AgentProcessingStage.Should().Be("verifyingRequest");
    }

    [Fact]
    public void A_finished_stage_is_reported_as_its_own_event()
    {
        var progress = new WorkspaceSuggestionProgressReader().Read(
            """{"type":"stageCompleted","customerWorkflowIdentifier":"run-1","processingStage":"verifyingRequest"}""");

        progress.Should().ContainSingle();
        progress[0].Should().BeOfType<WorkspaceSuggestionStageCompletedEvent>()
            .Which.AgentProcessingStage.Should().Be("verifyingRequest");
    }

    [Fact]
    public void A_half_arrived_object_is_not_reported()
    {
        var progress = new WorkspaceSuggestionProgressReader().Read(
            """{"type":"stageStarted","customerWorkflowIdentifier":"run-1","proces""");

        progress.Should().BeEmpty("a partial object is not an event yet");
    }

    [Fact]
    public void A_closed_object_is_reported_once_though_the_text_is_rescanned()
    {
        var reader = new WorkspaceSuggestionProgressReader();

        reader.Read(StageStarted).Should().ContainSingle();

        // The same text again is the same object, and rescanning must not report it a second time.
        reader.Read(StageStarted).Should().BeEmpty();

        var next = reader.Read(
            StageStarted + "\n"
            + """{"type":"retry","customerWorkflowIdentifier":"run-1","nextAttemptNumber":2,"maximumAttemptCount":3}""");

        next.Should().ContainSingle();
        next[0].Should().BeOfType<WorkspaceSuggestionRetryEvent>()
            .Which.NextAttemptNumber.Should().Be(2);
    }

    [Fact]
    public void A_candidate_is_reported_with_its_lines_totalled()
    {
        var progress = new WorkspaceSuggestionProgressReader().Read(
            """
            {"type":"candidate","customerWorkflowIdentifier":"run-1","approvedWorkspaceSetup":{"lines":[{"slot":"Desk","sku":"DSKB08XN4JDR","name":"Sit-Stand Desk","quantity":1,"amount":4200000}]}}
            """);

        var candidate = progress.Should().ContainSingle()
            .Which.Should().BeOfType<WorkspaceSuggestionCandidateApprovedEvent>().Which.Candidate;

        candidate.MonthlyTotal.Should().Be(4_200_000m);
        candidate.Lines[0].Slot.Should().Be(SlotId.Desk);
    }

    [Fact]
    public void The_ending_is_not_progress()
    {
        var progress = new WorkspaceSuggestionProgressReader().Read(
            """{"type":"completed","customerWorkflowIdentifier":"run-1","runStatus":"success","outcomeReason":null,"completedAttemptCount":2}""");

        progress.Should().BeEmpty("the ending belongs to the whole text, not to the stream of progress");
    }
}
