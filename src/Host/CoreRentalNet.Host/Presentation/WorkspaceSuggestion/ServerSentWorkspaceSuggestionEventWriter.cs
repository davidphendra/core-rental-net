using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;
using Microsoft.AspNetCore.Http;

namespace CoreRentalNet.Host.Presentation.WorkspaceSuggestion;

/// <summary>Writes a run's events to the response as server-sent events, in the one shape a browser reads.</summary>
/// <remarks>
/// <para>
/// The framing is the whole of this type's job, and it is separated from the endpoint so that the shape of an
/// event is stated once: an event name, one line of data, and the blank line that ends it. The run decides what
/// is said; this decides how it is put on the wire.
/// </para>
/// <para>
/// Nothing derived from the model reaches these methods. Every value written here is a constant the
/// application wrote or a value it has already checked, because a newline anywhere in the data would end the
/// frame early and deliver the rest of a sentence as a field.
/// </para>
/// </remarks>
internal sealed class ServerSentWorkspaceSuggestionEventWriter(HttpResponse httpResponse) : IWorkspaceSuggestionEventWriter
{
    /// <summary>The content type a browser reads as an event stream, and nothing else.</summary>
    public const string MediaType = "text/event-stream";

    /// <summary>Begins the stream: the headers are sent and buffering is turned off.</summary>
    /// <remarks>
    /// Both headers are the difference between streaming and pretending to. A cache may hold the whole response
    /// and hand it over at the end, and a proxy in front of the application buffers by default, so a frame that
    /// is written is still a frame the customer is not reading.
    /// </remarks>
    public static async Task<ServerSentWorkspaceSuggestionEventWriter> BeginAsync(
        HttpResponse httpResponse,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpResponse);

        httpResponse.Headers["Cache-Control"] = "no-cache";
        httpResponse.Headers["X-Accel-Buffering"] = "no";
        httpResponse.ContentType = MediaType;

        // Commits the headers and starts the response, so that a failure after this point can no longer be
        // answered with a status code - which is why the gate and the permission are checked before.
        await httpResponse.StartAsync(cancellationToken);

        return new ServerSentWorkspaceSuggestionEventWriter(httpResponse);
    }

    /// <summary>Writes one frame for one event, and says nothing about the event itself.</summary>
    public Task WriteAsync(WorkspaceSuggestionStreamEvent suggestionStreamEvent, CancellationToken cancellationToken)
        => suggestionStreamEvent switch
        {
            WorkspaceSuggestionStageStreamEvent stageStreamEvent
                => WriteFrameAsync("stage", stageStreamEvent.StageWords, cancellationToken),
            WorkspaceSuggestionTextStreamEvent textStreamEvent
                => WriteFrameAsync("text", textStreamEvent.NarrativeWords, cancellationToken),
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
