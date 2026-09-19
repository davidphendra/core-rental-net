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
        bool complete,
        CancellationToken cancellationToken)
    {
        await response.WriteAsync($"event: response.created\ndata: {Created(model)}\n\n", cancellationToken)
            .ConfigureAwait(false);
        await response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);

        var sequence = 1;
        var items = new List<string>();

        // One item per top-level object, which is how the workflow's two answers arrive: separate messages
        // rather than one document. The answer is written as it is - no waiting, and no piece cut smaller than
        // a message - so a run's duration is the application's and the transport's, and nothing here paces it.
        foreach (var line in body.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var itemId = $"msg_{items.Count + 1}";
            items.Add(itemId);

            await response.WriteAsync(
                    $"event: response.output_text.delta\ndata: {Delta(line + "\n", sequence++, itemId, items.Count - 1)}\n\n",
                    cancellationToken)
                .ConfigureAwait(false);
            await response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
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
