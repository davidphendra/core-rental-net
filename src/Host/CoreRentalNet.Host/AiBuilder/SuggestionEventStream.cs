namespace CoreRentalNet.Host.AiBuilder;

/// <summary>
/// Writes a run's events to the response as server-sent events, in the one shape a browser reads.
/// </summary>
/// <remarks>
/// <para>
/// The framing is the whole of this type's job, and it is separated from the endpoint so that the shape
/// of an event is stated once: an event name, one line of data, and the blank line that ends it. The
/// endpoint decides what is said; this decides how it is put on the wire.
/// </para>
/// <para>
/// Nothing derived from the model reaches these methods. Every value written here is a constant this
/// application wrote or a value it has already checked, because a newline anywhere in the data would end
/// the frame early and deliver the rest of a sentence as a field.
/// </para>
/// </remarks>
internal sealed class SuggestionEventStream(HttpResponse response)
{
    /// <summary>The content type a browser reads as an event stream, and nothing else.</summary>
    public const string MediaType = "text/event-stream";

    /// <summary>Begins the stream: the headers are sent and buffering is turned off.</summary>
    /// <remarks>
    /// Both headers are the difference between streaming and pretending to. A cache may hold the whole
    /// response and hand it over at the end, and a proxy in front of the application buffers by default,
    /// so a frame that is written is still a frame the customer is not reading. This is worth more than
    /// it looks: the point of the endpoint is that a long wait is filled honestly.
    /// </remarks>
    public static async Task<SuggestionEventStream> BeginAsync(
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(response);

        response.Headers["Cache-Control"] = "no-cache";
        response.Headers["X-Accel-Buffering"] = "no";
        response.ContentType = MediaType;

        // Commits the headers and starts the response, so that a failure after this point can no longer
        // be answered with a status code - which is why the guard and the permission are checked before.
        await response.StartAsync(cancellationToken);

        return new SuggestionEventStream(response);
    }

    /// <summary>The code a run that could not be made reports: no agent, no identity, or no transport.</summary>
    /// <remarks>
    /// A code rather than a sentence, and a stable one: the browser owns the words, so the difference
    /// between a failure and a refusal is decided in one place. The reason the agent gave is diagnostic and
    /// does not travel here.
    /// </remarks>
    public const string Unavailable = "unavailable";

    /// <summary>One app-owned stage line, in the customer's words.</summary>
    public Task StageAsync(string words, CancellationToken cancellationToken)
        => WriteAsync("stage", words, cancellationToken);

    /// <summary>One completed narrative field, already hygiened, in the model's words.</summary>
    public Task TextAsync(string words, CancellationToken cancellationToken)
        => WriteAsync("text", words, cancellationToken);

    /// <summary>The terminal answer, whole, as one frame.</summary>
    /// <remarks>
    /// Atomic by construction: it is written once, after the reader has seen the end of the agent's answer,
    /// so a customer is never shown a candidate half-arrived. A serialized result carries no newlines, which
    /// is what keeps it one frame.
    /// </remarks>
    public Task ResultAsync(string json, CancellationToken cancellationToken)
        => WriteAsync("result", json, cancellationToken);

    /// <summary>That the run could not be made, with the code the browser words.</summary>
    public Task FailedAsync(string code, CancellationToken cancellationToken)
        => WriteAsync("failed", code, cancellationToken);

    /// <summary>The answer was not one this application can honour, so there is nothing to show.</summary>
    /// <remarks>
    /// A failure rather than a refusal: the agent answered, and what it said does not survive being checked
    /// against the catalogue. The customer retries.
    /// </remarks>
    public const string Invalid = "invalid";

    /// <summary>Writes one frame and flushes it, so it is on the way rather than in a buffer.</summary>
    private async Task WriteAsync(string name, string data, CancellationToken cancellationToken)
    {
        await response.WriteAsync($"event: {name}\ndata: {data}\n\n", cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
    }
}
