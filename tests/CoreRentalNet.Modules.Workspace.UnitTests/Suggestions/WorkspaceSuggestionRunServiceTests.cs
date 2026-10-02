using AwesomeAssertions;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent.Events;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run.Events;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run.Handlers;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;
using CoreRentalNet.Modules.Workspace.Domain;
using Xunit;

namespace CoreRentalNet.Modules.Workspace.UnitTests.Suggestions;

public sealed class WorkspaceSuggestionRunServiceTests
{
    [Fact] // the answer, and one record, whichever way it ended
    public async Task A_run_streams_its_answer_and_writes_one_record()
    {
        var records = new RecordingWorkspaceSuggestionRunRecordWriter();
        var service = Service(records, WorkspaceSuggestionRunStateTests.Ready(
            WorkspaceSuggestionAnswerStatus.Suggested,
            [WorkspaceSuggestionRunStateTests.Candidate()]));

        var frames = await FramesOfAsync(service);

        // The run sends the answer and nothing else: the stages are the agent's to announce, because only the
        // agent knows how many attempts a run took.
        frames.Should().ContainSingle();
        frames[0].Should().BeOfType<WorkspaceSuggestionResultStreamEvent>();

        records.Records.Should().ContainSingle();
        records.Records[0].Verdict.Should().Be(WorkspaceSuggestionVerdict.Suggested);
        records.Records[0].CustomerId.Should().Be("CUSTOMER");
        records.Records[0].Query.Should().Be("a desk");
    }

    [Fact] // the customer's cancellation is an ending, not a failure
    public async Task A_cancelled_run_records_a_stopped_verdict()
    {
        var records = new RecordingWorkspaceSuggestionRunRecordWriter();
        var service = Service(records, WorkspaceSuggestionRunStateTests.Ready(
            WorkspaceSuggestionAnswerStatus.Suggested,
            [WorkspaceSuggestionRunStateTests.Candidate()]));

        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var readTheRun = async () =>
        {
            await foreach (var _ in service.StreamAsync(Payload(), "CUSTOMER", cancellation.Token))
            {
            }
        };

        // The cancellation leaves the run as the cancellation it is; what the endpoint does with it is the
        // endpoint's business, and the record it left behind is the run's.
        await readTheRun.Should().ThrowAsync<OperationCanceledException>();

        records.Records.Should().ContainSingle();
        records.Records[0].Verdict.Should().Be(WorkspaceSuggestionVerdict.Stopped);
    }

    [Fact] // a run a consumer walked away from is still a run that has to be explained
    public async Task A_run_the_consumer_stops_reading_still_writes_its_record()
    {
        var records = new RecordingWorkspaceSuggestionRunRecordWriter();
        var service = Service(records, new WorkspaceSuggestionNarrativeDeltaEvent("{\"rationale\":\"A tidy setup\"}"));

        // One completed field, so the run has produced a frame and has not ended: abandoning the enumeration now
        // is walking away mid-run rather than reading to the end.
        var frames = service.StreamAsync(Payload(), "CUSTOMER", CancellationToken.None).GetAsyncEnumerator();

        try
        {
            (await frames.MoveNextAsync()).Should().BeTrue("the run produced a frame before the consumer left it");
        }
        finally
        {
            await frames.DisposeAsync();
        }

        records.Records.Should().ContainSingle("a run that was started is a run that has to be explained");
    }

    private static async Task<List<WorkspaceSuggestionStreamEvent>> FramesOfAsync(
        WorkspaceSuggestionRunService service)
    {
        var frames = new List<WorkspaceSuggestionStreamEvent>();

        await foreach (var frame in service.StreamAsync(Payload(), "CUSTOMER", CancellationToken.None))
        {
            frames.Add(frame);
        }

        return frames;
    }

    private static WorkspaceSuggestionRunService Service(
        RecordingWorkspaceSuggestionRunRecordWriter records,
        params WorkspaceSuggestionEvent[] events)
        => new(
            new ScriptedWorkspaceSuggestionAgentAdapter(events),
            new WorkspaceSuggestionStreamProcessor(
            [
                new WorkspaceSuggestionNarrativeDeltaStreamEventHandler(),
                new WorkspaceSuggestionResultReadyStreamEventHandler(),
                new WorkspaceSuggestionUnavailableStreamEventHandler(),
            ]),
            records);

    private static WorkspaceSuggestionRequestPayload Payload()
        => new("run", "a desk", "IDR", null, []);
}
