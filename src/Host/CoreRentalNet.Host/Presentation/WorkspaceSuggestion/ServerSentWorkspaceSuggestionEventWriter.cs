using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run.Events;

namespace CoreRentalNet.Host.Presentation.WorkspaceSuggestion;

/// <summary>Writes a run's events to the response as server-sent events, in the one shape a browser reads.</summary>
/// <remarks>
/// <para>
/// The framing is the whole of this type's job, and it is separated from the endpoint so that the shape of an
/// event is stated once: an event name, one line of data, and the blank line that ends it. The run decides what
/// is said; this decides how it is put on the wire.
/// </para>
/// <para>
/// <b>Two operations, because the stream has two ends the endpoint has to drive.</b> Opening it is not the same
/// as writing to it - the headers commit the response and turn buffering off, which is what makes the difference
/// between streaming and pretending to - and it has to happen after the gate, the permission and the payload, so
/// that a refusal can still be a status code. Closing it is not an operation at all: every frame is flushed as it
/// is written, and the response ends when the endpoint returns.
/// </para>
/// <para>
/// Nothing derived from the model reaches these methods. Every value written here is a constant the
/// application wrote or a value it has already checked, because a newline anywhere in the data would end the
/// frame early and deliver the rest of a sentence as a field.
/// </para>
/// </remarks>
internal sealed class ServerSentWorkspaceSuggestionEventWriter(HttpResponse httpResponse)
{
    /// <summary>The content type a browser reads as an event stream, and nothing else.</summary>
    public const string MediaType = "text/event-stream";

    /// <summary>Begins the stream: the headers are sent and buffering is turned off.</summary>
    /// <remarks>
    /// Both headers are the difference between streaming and pretending to. A cache may hold the whole response
    /// and hand it over at the end, and a proxy in front of the application buffers by default, so a frame that
    /// is written is still a frame the customer is not reading.
    /// </remarks>
    public async Task BeginAsync(CancellationToken cancellationToken)
    {
        httpResponse.Headers["Cache-Control"] = "no-cache";
        httpResponse.Headers["X-Accel-Buffering"] = "no";

        // Commits the headers and starts the response, so that a failure after this point can no longer be
        // answered with a status code - which is why the gate and the permission are checked before.
        httpResponse.ContentType = MediaType;

        await httpResponse.StartAsync(cancellationToken);
    }

    /// <summary>Writes one frame for one event, and says nothing about the event itself.</summary>
    public Task WriteAsync(WorkspaceSuggestionStreamEventBase suggestionStreamEvent, CancellationToken cancellationToken)
        => suggestionStreamEvent switch
        {
            WorkspaceSuggestionStageStartedStreamEvent stageStartedStreamEvent
                => WriteFrameAsync("stage", stageStartedStreamEvent.StageWords, cancellationToken),
            WorkspaceSuggestionStageCompletedStreamEvent stageCompletedStreamEvent
                => WriteFrameAsync("stageCompleted", stageCompletedStreamEvent.StageWords, cancellationToken),
            WorkspaceSuggestionResultStreamEvent resultStreamEvent
                => WriteFrameAsync("result", WorkspaceSuggestionResultJson.Serialize(resultStreamEvent.ResultFrame), cancellationToken),
            WorkspaceSuggestionRetryStreamEvent retryStreamEvent
                => WriteFrameAsync("retry", retryStreamEvent.RetryWords, cancellationToken),
            WorkspaceSuggestionCandidateStreamEvent candidateStreamEvent
                => WriteFrameAsync("candidate", candidateStreamEvent.CandidateWords, cancellationToken),
            WorkspaceSuggestionFailedStreamEvent failedStreamEvent
                => WriteFrameAsync("failed", failedStreamEvent.FailureCode, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(
                nameof(suggestionStreamEvent), suggestionStreamEvent, "No frame is written for this event."),
        };

    /// <summary>Writes one frame and flushes it, so it is on the way rather than in a buffer.</summary>
    private async Task WriteFrameAsync(string frameName, string frameData, CancellationToken cancellationToken)
    {
        await httpResponse.WriteAsync($"event: {frameName}\ndata: {frameData}\n\n", cancellationToken);
        await httpResponse.Body.FlushAsync(cancellationToken);
    }
}
