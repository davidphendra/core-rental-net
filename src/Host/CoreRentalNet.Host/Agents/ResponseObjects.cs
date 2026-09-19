using System.Text;
using System.Text.Json;

namespace CoreRentalNet.Host.Agents;

/// <summary>Every complete top-level JSON object in a streamed answer, in the order they arrived.</summary>
/// <remarks>
/// <para>
/// The answer is a <b>sequence of values, not one document</b>, and there are now three of them: the
/// rephraser's specification, the suggestor's result, and the run's cost. So this reads all of them and the
/// caller picks by what it is looking for — the result is the object with a <c>status</c>, the cost is the
/// object with a <c>runUsage</c>.
/// </para>
/// <para>
/// <b>Picking by property, not by position and not by "does it deserialize".</b> "The last object" stopped
/// being true the moment the cost was appended after the result. Deserializing each object and keeping the
/// one that succeeds is worse than either: an enum with a missing value deserializes to its <em>first</em>
/// member, so an object with no <c>status</c> at all would quietly be read as a valid suggestion.
/// </para>
/// <para>
/// A half-arrived tail is a stop, not a failure: the reader is not final, so an incomplete token ends the
/// scan cleanly and everything before it stands.
/// </para>
/// </remarks>
internal static class ResponseObjects
{
    public static IReadOnlyList<JsonElement> All(string text)
    {
        var found = new List<JsonElement>();

        if (string.IsNullOrWhiteSpace(text))
        {
            return found;
        }

        var reader = new Utf8JsonReader(
            Encoding.UTF8.GetBytes(text),
            isFinalBlock: false,
            state: new JsonReaderState(new JsonReaderOptions { AllowMultipleValues = true }));

        try
        {
            while (reader.Read())
            {
                if (reader.TokenType is not JsonTokenType.StartObject)
                {
                    continue;
                }

                using var document = JsonDocument.ParseValue(ref reader);

                found.Add(document.RootElement.Clone());
            }
        }
        catch (JsonException)
        {
            // The model is mid-thought or the transport appended something that is not JSON. Everything
            // collected before this point stands.
        }

        return found;
    }

    public static bool Has(JsonElement element, string property)
        => element.ValueKind is JsonValueKind.Object && element.TryGetProperty(property, out _);

    public static T? Read<T>(JsonElement element)
        where T : class
    {
        try
        {
            return element.Deserialize<T>(SuggestionJson.Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
