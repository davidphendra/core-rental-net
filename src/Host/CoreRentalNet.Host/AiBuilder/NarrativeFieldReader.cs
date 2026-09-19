using System.Text;
using System.Text.Json;

namespace CoreRentalNet.Host.AiBuilder;

/// <summary>Turns the agent's streamed JSON into the model's words, and nothing else.</summary>
/// <remarks>
/// <para>
/// The agent answers with structured JSON, so what arrives is braces, keys and escaped quotes. This watches
/// the arriving text and emits a narrative field the moment its JSON string closes. <b>Raw JSON is never
/// rendered, and neither is a partial value.</b>
/// </para>
/// <para>
/// <b>The stream carries two top-level objects, not one.</b> The sequential workflow answers with the
/// rephraser's specification first and the suggestor's result last, and both arrive as
/// <c>response.output_text.delta</c> events on separate message items. So the reader parses a sequence of
/// values rather than a document — established by running the host, not assumed.
/// </para>
/// <para>
/// The text is re-scanned from the start each time rather than resuming mid-token. That is deliberate: the
/// answer is a couple of kilobytes and <see cref="Utf8JsonReader"/> with <c>isFinalBlock: false</c> stops
/// cleanly at an incomplete token and never throws for one, so rescanning is both obviously correct and
/// cheap enough that resuming is not worth the bookkeeping.
/// </para>
/// </remarks>
internal sealed class NarrativeFieldReader
{
    private static readonly JsonReaderOptions Options = new() { AllowMultipleValues = true };

    private readonly StringBuilder _buffer = new();

    /// <summary>How many fields the buffer has produced so far, so each is emitted exactly once.</summary>
    private int _emitted;

    /// <summary>Emits whatever narrative fields have closed since the previous fragment.</summary>
    public IReadOnlyList<NarrativeField> Feed(string fragment)
    {
        if (!string.IsNullOrEmpty(fragment))
        {
            _buffer.Append(fragment);
        }

        var complete = Scan();
        var fresh = complete.Skip(_emitted).ToArray();

        _emitted = complete.Count;

        return fresh;
    }

    /// <summary>Every narrative field the buffer has produced, in the order the model wrote them.</summary>
    private List<NarrativeField> Scan()
    {
        var fields = new List<NarrativeField>();
        var reader = TokenReader();
        string? key = null;

        try
        {
            while (reader.Read())
            {
                key = Collect(reader, key, fields);
            }
        }
        catch (JsonException)
        {
            // A tail that is not JSON at all - the model is mid-thought, not wrong. Everything collected
            // before this point stands, and the next fragment gets another chance.
        }

        return fields;
    }

    /// <summary>Records one token, and returns the property name that is still in scope afterwards.</summary>
    private static string? Collect(Utf8JsonReader reader, string? key, List<NarrativeField> fields)
    {
        if (reader.TokenType is JsonTokenType.PropertyName)
        {
            return reader.GetString();
        }

        if (reader.TokenType is JsonTokenType.String && Kind(key) is { } kind)
        {
            fields.Add(new NarrativeField(kind, reader.GetString() ?? string.Empty));
        }

        // Only the token immediately after a property name can be that property's value.
        return null;
    }

    /// <summary>
    /// A reader over everything received so far. <c>isFinalBlock: false</c> is what makes a half-arrived
    /// value a stop rather than an exception, and <c>AllowMultipleValues</c> is what makes the second
    /// top-level object parse at all.
    /// </summary>
    private Utf8JsonReader TokenReader()
        => new(
            Encoding.UTF8.GetBytes(_buffer.ToString()),
            isFinalBlock: false,
            state: new JsonReaderState(Options));

    private static NarrativeFieldKind? Kind(string? key) => key switch
    {
        "rationale" => NarrativeFieldKind.Rationale,
        "why" => NarrativeFieldKind.Why,
        _ => null,
    };
}
