using System.Diagnostics;
using System.Runtime.CompilerServices;
using CoreRentalNet.BuildingBlocks.Application.Telemetry;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run.Events;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Records;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;

/// <summary>The one place a suggestion run is executed: the service the endpoint delegates to.</summary>
/// <remarks>
/// <para>
/// It owns the run's boundary: the pipeline it drives, the one cancellation it maps to an ending, and the record
/// it writes whichever way the run ended. It holds no state of its own - the state is created per run - so it is
/// safe to register per request.
/// </para>
/// <para>
/// <b>The ending is decided in a <c>finally</c> because a <c>yield</c> cannot sit inside a <c>try</c> that
/// catches.</b> The record is written when the enumeration ends or is disposed, which covers all four ways a run
/// stops: the agent answered, the agent failed, the customer cancelled, and the caller walked away.
/// </para>
/// </remarks>
public sealed class WorkspaceSuggestionRunService(
    IWorkspaceSuggestionAgentAdapter suggestionAgentAdapter,
    IWorkspaceSuggestionStreamProcessor suggestionStreamProcessor,
    IWorkspaceSuggestionRunRecordWriter suggestionRunRecordWriter) : IWorkspaceSuggestionRunService
{
    public async IAsyncEnumerable<WorkspaceSuggestionStreamEventBase> StreamAsync(
        WorkspaceSuggestionRequestPayload suggestionRequestPayload,
        string hashedCustomerIdentity,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var startedTimestamp = Stopwatch.GetTimestamp();
        BusinessTelemetry.SuggestionRunsStarted.Add(1);
        var suggestionRunState = new WorkspaceSuggestionRunState(suggestionRequestPayload.Query);

        try
        {
            // The stages are the agent's to announce: it knows where a run went, including a retry the
            // application cannot predict. The application only supplies the words.
            await foreach (var suggestionStreamEvent in suggestionStreamProcessor.ProcessAsync(
                suggestionAgentAdapter.StreamAsync(suggestionRequestPayload, cancellationToken),
                suggestionRunState,
                cancellationToken))
            {
                yield return suggestionStreamEvent;
            }
        }
        finally
        {
            // One cancellation, two meanings, decided by whose token was signalled. A run that reached no verdict
            // of its own is already recorded as unavailable, which is the ledger's own default.
            if (cancellationToken.IsCancellationRequested)
            {
                suggestionRunState.MarkEnded(WorkspaceSuggestionVerdict.Stopped);
            }

            var suggestionRunRecord = suggestionRunState.CreateRunRecord(hashedCustomerIdentity, startedTimestamp);
            suggestionRunRecordWriter.WriteRunRecord(suggestionRunRecord);

            // Read from the record the run already writes, so the metric and the ledger cannot describe
            // different endings. The verdict is a closed set, which is what makes it safe as a dimension, and
            // Unavailable climbing is the number that says the agent is degrading.
            BusinessTelemetry.SuggestionRunsEnded.Add(
                1,
                new KeyValuePair<string, object?>("verdict", suggestionRunRecord.Verdict.ToString()));
            BusinessTelemetry.SuggestionRunDuration.Record(suggestionRunRecord.LatencyMilliseconds / 1000.0);
        }
    }
}
