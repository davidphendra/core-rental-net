using AwesomeAssertions;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;
using Xunit;

namespace CoreRentalNet.Modules.Workspace.UnitTests.Suggestions;

public sealed class WorkspaceSuggestionStreamProcessorTests
{
    [Fact] // each event reaches the one handler that owns it, in order
    public async Task Every_event_is_applied_by_its_own_handler()
    {
        var state = new WorkspaceSuggestionRunState("a desk");
        var writer = new RecordingWorkspaceSuggestionEventWriter();

        await Processor().ProcessAgentEventStreamAsync(
            Events(
                new WorkspaceSuggestionNarrativeDeltaAgentEvent("{\"rationale\":\"A tidy setup\"}"),
                WorkspaceSuggestionRunStateTests.Ready(WorkspaceSuggestionAnswerStatus.Suggested, [WorkspaceSuggestionRunStateTests.Candidate()])),
            state,
            writer,
            CancellationToken.None);

        writer.Events.Should().HaveCount(2);
        writer.Events[0].Should().BeOfType<WorkspaceSuggestionTextStreamEvent>();
        writer.Events[1].Should().BeOfType<WorkspaceSuggestionResultStreamEvent>();
        state.HasEnded.Should().BeTrue();
    }

    [Fact] // the processor stops at the ending rather than reading on
    public async Task The_processor_stops_once_the_run_has_ended()
    {
        var state = new WorkspaceSuggestionRunState("a desk");
        var writer = new RecordingWorkspaceSuggestionEventWriter();

        await Processor().ProcessAgentEventStreamAsync(
            Events(
                WorkspaceSuggestionRunStateTests.Ready(WorkspaceSuggestionAnswerStatus.Suggested, [WorkspaceSuggestionRunStateTests.Candidate()]),
                new WorkspaceSuggestionNarrativeDeltaAgentEvent("{\"rationale\":\"never read\"}")),
            state,
            writer,
            CancellationToken.None);

        writer.Events.Should().ContainSingle().Which.Should().BeOfType<WorkspaceSuggestionResultStreamEvent>();
    }

    [Fact]
    public async Task An_unavailable_agent_writes_the_unavailable_code()
    {
        var state = new WorkspaceSuggestionRunState("a desk");
        var writer = new RecordingWorkspaceSuggestionEventWriter();

        await Processor().ProcessAgentEventStreamAsync(
            Events(new WorkspaceSuggestionUnavailableAgentEvent("no agent")),
            state,
            writer,
            CancellationToken.None);

        writer.Events.Should().ContainSingle()
            .Which.Should().BeOfType<WorkspaceSuggestionFailedStreamEvent>()
            .Which.FailureCode.Should().Be(WorkspaceSuggestionFailureCode.Unavailable);
    }

    private static WorkspaceSuggestionStreamProcessor Processor()
        => new(
        [
            new WorkspaceSuggestionNarrativeDeltaStreamEventHandler(),
            new WorkspaceSuggestionResultReadyStreamEventHandler(),
            new WorkspaceSuggestionUnavailableStreamEventHandler(),
        ]);

    private static async IAsyncEnumerable<WorkspaceSuggestionAgentEvent> Events(params WorkspaceSuggestionAgentEvent[] events)
    {
        foreach (var raised in events)
        {
            yield return raised;
        }

        await Task.CompletedTask;
    }
}
