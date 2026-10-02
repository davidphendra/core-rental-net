using AwesomeAssertions;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent.Events;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run.Events;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run.Handlers;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;
using Xunit;

namespace CoreRentalNet.Modules.Workspace.UnitTests.Suggestions;

public sealed class WorkspaceSuggestionStreamProcessorTests
{
    [Fact] // each event reaches the one handler that owns it, in order
    public async Task Every_event_is_applied_by_its_own_handler()
    {
        var state = new WorkspaceSuggestionRunState("a desk");

        var frames = await FramesOfAsync(
            state,
            new WorkspaceSuggestionNarrativeDeltaEvent("{\"rationale\":\"A tidy setup\"}"),
            WorkspaceSuggestionRunStateTests.Ready(
                WorkspaceSuggestionAnswerStatus.Suggested,
                [WorkspaceSuggestionRunStateTests.Candidate()]));

        frames.Should().HaveCount(2);
        frames[0].Should().BeOfType<WorkspaceSuggestionTextStreamEvent>();
        frames[1].Should().BeOfType<WorkspaceSuggestionResultStreamEvent>();
        state.HasEnded.Should().BeTrue();
    }

    [Fact] // the processor stops at the ending rather than reading on
    public async Task The_processor_stops_once_the_run_has_ended()
    {
        var state = new WorkspaceSuggestionRunState("a desk");

        var frames = await FramesOfAsync(
            state,
            WorkspaceSuggestionRunStateTests.Ready(
                WorkspaceSuggestionAnswerStatus.Suggested,
                [WorkspaceSuggestionRunStateTests.Candidate()]),
            new WorkspaceSuggestionNarrativeDeltaEvent("{\"rationale\":\"never read\"}"));

        frames.Should().ContainSingle().Which.Should().BeOfType<WorkspaceSuggestionResultStreamEvent>();
    }

    [Fact]
    public async Task An_unavailable_agent_yields_the_unavailable_code()
    {
        var state = new WorkspaceSuggestionRunState("a desk");

        var frames = await FramesOfAsync(state, new WorkspaceSuggestionUnavailableEvent("no agent"));

        frames.Should().ContainSingle()
            .Which.Should().BeOfType<WorkspaceSuggestionFailedStreamEvent>()
            .Which.FailureCode.Should().Be(WorkspaceSuggestionFailureCode.Unavailable);
    }

    private static async Task<List<WorkspaceSuggestionStreamEvent>> FramesOfAsync(
        WorkspaceSuggestionRunState state,
        params WorkspaceSuggestionEvent[] agentEvents)
    {
        var frames = new List<WorkspaceSuggestionStreamEvent>();

        await foreach (var frame in Processor().ProcessAsync(Events(agentEvents), state, CancellationToken.None))
        {
            frames.Add(frame);
        }

        return frames;
    }

    private static WorkspaceSuggestionStreamProcessor Processor()
        => new(
        [
            new WorkspaceSuggestionNarrativeDeltaStreamEventHandler(),
            new WorkspaceSuggestionResultReadyStreamEventHandler(),
            new WorkspaceSuggestionUnavailableStreamEventHandler(),
        ]);

    private static async IAsyncEnumerable<WorkspaceSuggestionEvent> Events(params WorkspaceSuggestionEvent[] events)
    {
        foreach (var raised in events)
        {
            yield return raised;
        }

        await Task.CompletedTask;
    }
}
