using System.Text.Json;

namespace CoreRentalNet.E2E.LocalAgent;

/// <summary>Writes an answer as the Responses protocol's event stream.</summary>
/// <remarks>
/// <para>
/// The shape is the documented one: numbered <c>response.output_text.delta</c> events carrying a piece of
/// text, then one <c>response.completed</c> carrying the finished response. Written by hand because the
/// format is the protocol's rather than the client's — the client's own reader is internal, which is exactly
/// why a stand-in is worth having: it is the other end of the wire, written from the specification.
/// </para>
/// <para>
/// <b>One top-level object per item.</b> The answer is a sequence of values rather than one document — the
/// rephraser's specification, then the suggestor's result — and on the real wire each arrives as its own
/// message item. The stand-in reproduces that, because the boundary is the one fact about this protocol the
/// application has already had to learn the hard way.
/// </para>
/// </remarks>
internal static class SseWriter
{
    /// <summary>Writes one complete answer: the text in pieces, then the response it completed.</summary>
    /// <param name="complete">
    /// False for the scenario that stops mid-answer: the pieces are written and the response is never
    /// completed, which is what a run that failed after saying something looks like on the wire.
    /// </param>
    public static async Task WriteAsync(
        HttpResponse response,
        string model,
        string body,
        int delayMilliseconds,
        bool complete,
        CancellationToken cancellationToken)
    {
        await response.WriteAsync($"event: response.created\ndata: {Created(model)}\n\n", cancellationToken)
            .ConfigureAwait(false);
        await response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);

        var sequence = 1;
        var items = new List<string>();

        // One item per top-level object, and each object cut into pieces. A fixture's object is a whole line
        // by construction, which is also how the real workflow's two answers arrive: separate messages, not
        // one document. The pieces are what make a run watchable - a single delta per object would arrive in
        // one frame, and "the customer reads it as it happens" would be true of a burst rather than a stream.
        foreach (var line in body.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var itemId = $"msg_{items.Count + 1}";
            items.Add(itemId);

            foreach (var piece in Pieces(line))
            {
                await Pause(delayMilliseconds, cancellationToken).ConfigureAwait(false);

                await response.WriteAsync(
                        $"event: response.output_text.delta\ndata: {Delta(piece, sequence++, itemId, items.Count - 1)}\n\n",
                        cancellationToken)
                    .ConfigureAwait(false);
                await response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        if (complete)
        {
            await response.WriteAsync(
                    $"event: response.completed\ndata: {Completed(model, body, sequence, items)}\n\n",
                    cancellationToken)
                .ConfigureAwait(false);
            await response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// One object's text in pieces.
    /// </summary>
    /// <remarks>
    /// The trailing newline is kept, because it is what separates one top-level object from the next in the
    /// text the application accumulates - a real workflow's two answers are newline-delimited on the wire, and
    /// a fixture that dropped it would be read as one malformed value instead of two values.
    /// </remarks>
    private static IEnumerable<string> Pieces(string line)
    {
        const int size = 120;

        for (var start = 0; start < line.Length; start += size)
        {
            var piece = line.Substring(start, Math.Min(size, line.Length - start));

            yield return start + size >= line.Length ? piece + "\n" : piece;
        }
    }

    /// <summary>Waits, so a run has time to be watched - and to be cancelled.</summary>
    /// <remarks>
    /// The wait is cancellable, and that matters: a client that hangs up mid-stream has to stop the stand-in,
    /// or the fixture would carry on writing to a socket nobody is reading.
    /// </remarks>
    private static Task Pause(int delayMilliseconds, CancellationToken cancellationToken)
        => delayMilliseconds > 0
            ? Task.Delay(delayMilliseconds, cancellationToken)
            : Task.CompletedTask;

    private static string Created(string model)
        => Json(new { type = "response.created", sequence_number = 0, response = Envelope(model, "in_progress") });

    private static string Delta(string piece, int sequence, string itemId, int outputIndex)
        => Json(new
        {
            type = "response.output_text.delta",
            sequence_number = sequence,
            item_id = itemId,
            output_index = outputIndex,
            content_index = 0,
            delta = piece,
        });

    private static string Completed(string model, string text, int sequence, IReadOnlyList<string> items)
        => Json(new
        {
            type = "response.completed",
            sequence_number = sequence,
            response = Envelope(model, "completed", text, items),
        });

    private static object Envelope(
        string model,
        string status,
        string? text = null,
        IReadOnlyList<string>? items = null)
        => new
        {
            id = "resp_stand_in",
            @object = "response",
            created_at = 0,
            status,
            model,
            output = text is null || items is null
                ? []
                : items.Select(itemId => new
                {
                    id = itemId,
                    type = "message",
                    status = "completed",
                    role = "assistant",
                    content = new object[]
                    {
                        new { type = "output_text", text, annotations = Array.Empty<object>() },
                    },
                }).ToArray(),
            parallel_tool_calls = true,
            tool_choice = "auto",
            tools = Array.Empty<object>(),
        };

    private static string Json(object value)
        => JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        });
}
