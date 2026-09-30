using System.Diagnostics;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Records;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;

/// <summary>The one place a suggestion run is executed: the service the endpoint delegates to.</summary>
/// <remarks>
/// It owns the run's boundary: the stages it writes, the pipeline it drives, the one cancellation it maps to
/// an ending, and the record it writes whichever way the run ended. It holds no state of its own - the state
/// is created per run - so it is safe to register per request.
/// </remarks>
public sealed class WorkspaceSuggestionRunService(
    IWorkspaceSuggestionAgentAdapter suggestionAgentAdapter,
    IWorkspaceSuggestionStreamProcessor suggestionStreamProcessor,
    IWorkspaceSuggestionRunRecordWriter suggestionRunRecordWriter) : IWorkspaceSuggestionRunService
{
    public async Task RunSuggestionAsync(
        WorkspaceSuggestionRequestPayload suggestionRequestPayload,
        string callerAccessToken,
        IWorkspaceSuggestionEventWriter suggestionEventWriter,
        string hashedCustomerIdentity,
        CancellationToken cancellationToken)
    {
        var startedTimestamp = Stopwatch.GetTimestamp();
        var suggestionRunState = new WorkspaceSuggestionRunState(suggestionRequestPayload.Query);

        try
        {
            // The stages are the agent's to announce: it knows where a run went, including a retry the
            // application cannot predict. The application only supplies the words.
            await suggestionStreamProcessor.ProcessAgentEventStreamAsync(
                suggestionAgentAdapter.StreamSuggestionAsync(
                    suggestionRequestPayload,
                    callerAccessToken,
                    cancellationToken),
                suggestionRunState,
                suggestionEventWriter,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // One cancellation, two meanings, decided by whose token was signalled.
            suggestionRunState.MarkEnded(cancellationToken.IsCancellationRequested
                ? WorkspaceSuggestionVerdict.Stopped
                : WorkspaceSuggestionVerdict.Unavailable);
        }
        finally
        {
            suggestionRunRecordWriter.WriteRunRecord(
                suggestionRunState.CreateRunRecord(hashedCustomerIdentity, startedTimestamp));
        }
    }
}
