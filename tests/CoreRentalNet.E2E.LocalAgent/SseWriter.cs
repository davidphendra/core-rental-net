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
    public static async Task WriteAsync(
        HttpResponse response,
        string model,
        string body,
        int delayMilliseconds,
        CancellationToken cancellationToken)
    {
        await response.WriteAsync($"event: response.created\ndata: {Created(model)}\n\n", cancellationToken)
            .ConfigureAwait(false);
        await response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);

        var sequence = 1;
        var items = new List<string>();

        // One item per top-level object. A fixture's object is a whole line by construction, which is also how
        // the real workflow's two answers arrive: separate messages, not one document.
        foreach (var line in body.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            await Pause(delayMilliseconds, cancellationToken).ConfigureAwait(false);

            var itemId = $"msg_{items.Count + 1}";
            items.Add(itemId);

            await response.WriteAsync(
                    $"event: response.output_text.delta\ndata: {Delta(line + "\n", sequence++, itemId, items.Count - 1)}\n\n",
                    cancellationToken)
                .ConfigureAwait(false);
            await response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        await response.WriteAsync(
                $"event: response.completed\ndata: {Completed(model, body, sequence, items)}\n\n",
                cancellationToken)
            .ConfigureAwait(false);
        await response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
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
