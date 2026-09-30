using AwesomeAssertions;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Domain;
using CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;
using Xunit;

namespace CoreRentalNet.IntegrationTests.Suggestions;

/// <summary>
/// The reader that turns the agent's streamed wire shape into the application's vocabulary.
/// </summary>
/// <remarks>
/// It is the seam the camel-case regression passed through unseen: the agent's events were being written with a
/// camel-cased slot, and the reader's <see cref="SlotId"/> converter would have refused every one of them on the
/// first real run. Nothing asserted this crossing, so nothing failed - which is why these tests are written
/// against the contract's own words rather than against the agent's serializer.
/// </remarks>
public sealed class MicrosoftFoundrySuggestionAnswerReaderTests
{
    private const string StreamedAnswer =
        """
        {"type":"stageStarted","customerWorkflowIdentifier":"run-1","processingStage":"verifyingRequest"}
        {"type":"stageStarted","customerWorkflowIdentifier":"run-1","processingStage":"composingWorkspaceSetups"}
        {"type":"retry","customerWorkflowIdentifier":"run-1","nextAttemptNumber":2,"maximumAttemptCount":3}
        {"type":"candidate","customerWorkflowIdentifier":"run-1","approvedWorkspaceSetup":{"lines":[{"slot":"Desk","sku":"DSKB08XN4JDR","name":"Sit-Stand Desk","quantity":1,"amount":4200000,"why":"a stable surface"}],"rationale":"A calm, focused setup."}}
        {"type":"completed","customerWorkflowIdentifier":"run-1","runStatus":"success","outcomeReason":null,"completedAttemptCount":2}
        """;

    [Fact]
    public void The_answer_is_read_out_of_the_event_stream()
    {
        var streamedAnswer = MicrosoftFoundrySuggestionAnswerReader.ReadStreamedAnswer(StreamedAnswer);

        streamedAnswer.Should().NotBeNull();
        streamedAnswer!.Answer.Status.Should().Be(WorkspaceSuggestionAnswerStatus.Suggested);
        streamedAnswer.Answer.Candidates.Should().ContainSingle();
        streamedAnswer.Answer.Candidates[0].MonthlyTotal.Should().Be(4_200_000m);
        streamedAnswer.Answer.Candidates[0].Rationale.Should().Be("A calm, focused setup.");
    }

    [Fact]
    public void A_slot_is_read_in_the_contracts_own_words()
    {
        var streamedAnswer = MicrosoftFoundrySuggestionAnswerReader.ReadStreamedAnswer(StreamedAnswer);

        // "Desk", not "desk": the contract's vocabulary is PascalCase, and a reader that only accepted one
        // spelling would fail on the other without anything else noticing.
        streamedAnswer!.Answer.Candidates[0].Lines[0].Slot.Should().Be(SlotId.Desk);
        streamedAnswer.Answer.Candidates[0].Lines[0].Sku.Should().Be("DSKB08XN4JDR");
    }

    [Fact]
    public void The_stages_and_the_retries_are_kept_beside_the_answer()
    {
        var streamedAnswer = MicrosoftFoundrySuggestionAnswerReader.ReadStreamedAnswer(StreamedAnswer);

        streamedAnswer!.ProcessingStages.Should().Equal("verifyingRequest", "composingWorkspaceSetups");
        streamedAnswer.RetryAttempts.Should().ContainSingle();
        streamedAnswer.RetryAttempts[0].NextAttemptNumber.Should().Be(2);
        streamedAnswer.RetryAttempts[0].MaximumAttemptCount.Should().Be(3);
    }

    [Fact]
    public void A_rejected_run_is_a_refusal_with_no_candidates()
    {
        const string rejected =
            """
            {"type":"completed","customerWorkflowIdentifier":"run-1","runStatus":"rejected","outcomeReason":"not about a workspace","completedAttemptCount":0}
            """;

        var streamedAnswer = MicrosoftFoundrySuggestionAnswerReader.ReadStreamedAnswer(rejected);

        streamedAnswer!.Answer.Status.Should().Be(WorkspaceSuggestionAnswerStatus.NotWorkspace);
        streamedAnswer.Answer.Candidates.Should().BeEmpty();
    }

    [Fact]
    public void An_unavailable_run_is_not_a_refusal()
    {
        const string unavailable =
            """
            {"type":"completed","customerWorkflowIdentifier":"run-1","runStatus":"unavailable","outcomeReason":"none fitted","completedAttemptCount":3}
            """;

        var streamedAnswer = MicrosoftFoundrySuggestionAnswerReader.ReadStreamedAnswer(unavailable);

        streamedAnswer!.Answer.Status.Should().Be(WorkspaceSuggestionAnswerStatus.CatalogueUnavailable);
    }

    [Fact]
    public void A_stream_that_never_ended_is_not_an_answer()
    {
        // A truncated stream carried progress but no ending, so there is nothing to show and the run is reported
        // as unavailable rather than as a setup-less success.
        const string noEnding =
            """
            {"type":"stageStarted","customerWorkflowIdentifier":"run-1","processingStage":"verifyingRequest"}
            """;

        MicrosoftFoundrySuggestionAnswerReader.ReadStreamedAnswer(noEnding).Should().BeNull();
    }
}
