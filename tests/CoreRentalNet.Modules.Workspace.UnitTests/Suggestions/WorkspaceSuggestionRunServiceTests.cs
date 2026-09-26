using AwesomeAssertions;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;
using CoreRentalNet.Modules.Workspace.Domain;
using Xunit;

namespace CoreRentalNet.Modules.Workspace.UnitTests.Suggestions;

public sealed class WorkspaceSuggestionRunServiceTests
{
    [Fact] // the stages, then the answer; and one record, whichever way it ended
    public async Task A_run_writes_its_stages_then_its_answer_and_one_record()
    {
        var writer = new RecordingWorkspaceSuggestionEventWriter();
        var records = new RecordingWorkspaceSuggestionRunRecordWriter();
        var service = Service(records, WorkspaceSuggestionRunStateTests.Ready(
            WorkspaceSuggestionAnswerStatus.Suggested,
            [WorkspaceSuggestionRunStateTests.Candidate()]));

        await service.RunSuggestionAsync(Payload(), writer, "CUSTOMER", CancellationToken.None);

        writer.Events[0].Should().BeOfType<WorkspaceSuggestionStageStreamEvent>();
        writer.Events[1].Should().BeOfType<WorkspaceSuggestionStageStreamEvent>();
        writer.Events[2].Should().BeOfType<WorkspaceSuggestionResultStreamEvent>();

        records.Records.Should().ContainSingle();
        records.Records[0].Verdict.Should().Be(WorkspaceSuggestionVerdict.Suggested);
        records.Records[0].CustomerId.Should().Be("CUSTOMER");
        records.Records[0].Query.Should().Be("a desk");
    }

    [Fact] // the customer's cancellation is an ending, not a failure
    public async Task A_cancelled_run_records_a_stopped_verdict()
    {
        var writer = new RecordingWorkspaceSuggestionEventWriter();
        var records = new RecordingWorkspaceSuggestionRunRecordWriter();
        var service = Service(records, WorkspaceSuggestionRunStateTests.Ready(
            WorkspaceSuggestionAnswerStatus.Suggested,
            [WorkspaceSuggestionRunStateTests.Candidate()]));

        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await service.RunSuggestionAsync(Payload(), writer, "CUSTOMER", cancellation.Token);

        records.Records.Should().ContainSingle();
        records.Records[0].Verdict.Should().Be(WorkspaceSuggestionVerdict.Stopped);
    }

    private static WorkspaceSuggestionRunService Service(
        RecordingWorkspaceSuggestionRunRecordWriter records,
        params WorkspaceSuggestionAgentEvent[] events)
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
        => new("run", "a desk", "IDR", null, [], null);
}
