using System.Text.Json;

namespace CoreRentalNet.E2E.LocalAgent;

/// <summary>
/// Writes the agent's answer as the Responses protocol's event stream.
/// </summary>
/// <remarks>
/// <para>
/// The shape is the documented one: numbered <c>response.output_text.delta</c> events carrying a piece of
/// text, then one <c>response.completed</c> carrying the finished response. It is written by hand because
/// the format is the protocol's, not the client's - the client's own reader is internal, which is exactly
/// why a stand-in is worth having: it is the other end of the wire, written from the specification.
/// </para>
/// <para>
/// Text is sent in pieces rather than whole, because a real agent streams and a stand-in that sent one
/// piece would leave the accumulation across chunks untested - the case that fails on the runs with the
/// most output.
/// </para>
/// </remarks>
internal static class SseWriter
{
    private const int Sequence = 0;

    /// <summary>Writes one complete answer: the text in pieces, then the response it completed.</summary>
    public static async Task WriteAsync(
        HttpResponse response,
        string model,
        string text,
        CancellationToken cancellationToken)
    {
        await response.WriteAsync($"event: response.created\ndata: {Created(model)}\n\n", cancellationToken)
            .ConfigureAwait(false);
        await response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);

        var sequence = 1;

        foreach (var piece in Pieces(text))
        {
            await response.WriteAsync($"event: response.output_text.delta\ndata: {Delta(piece, sequence++)}\n\n", cancellationToken)
                .ConfigureAwait(false);
            await response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        var completed = Completed(model, text, sequence);

        await response.WriteAsync($"event: response.completed\ndata: {completed}\n\n", cancellationToken)
            .ConfigureAwait(false);
        await response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// The text in pieces, cut on line boundaries.
    /// </summary>
    /// <remarks>
    /// Line-sized pieces rather than a fixed character count, so that one piece is one contract message
    /// and the accumulation being tested is real accumulation rather than an artefact of where the cut
    /// happened to fall.
    /// </remarks>
    private static IEnumerable<string> Pieces(string text)
    {
        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            yield return line + "\n";
        }
    }

    private static string Created(string model)
        => Json(new { type = "response.created", sequence_number = Sequence, response = Envelope(model, "in_progress") });

    private static string Delta(string piece, int sequence)
        => Json(new
        {
            type = "response.output_text.delta",
            sequence_number = sequence,
            item_id = "msg_stand_in",
            output_index = 0,
            content_index = 0,
            delta = piece,
        });

    private static string Completed(string model, string text, int sequence)
        => Json(new
        {
            type = "response.completed",
            sequence_number = sequence,
            response = Envelope(model, "completed", text),
        });

    private static object Envelope(string model, string status, string? text = null)
        => new
        {
            id = "resp_stand_in",
            @object = "response",
            created_at = 0,
            status,
            model,
            output = text is null
                ? []
                : new object[]
                {
                    new
                    {
                        id = "msg_stand_in",
                        type = "message",
                        status = "completed",
                        role = "assistant",
                        content = new object[]
                        {
                            new { type = "output_text", text, annotations = Array.Empty<object>() },
                        },
                    },
                },
            parallel_tool_calls = true,
            tool_choice = "auto",
            tools = Array.Empty<object>(),
        };

    private static string Json(object value)
        => JsonSerializer.Serialize(value, new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
}
