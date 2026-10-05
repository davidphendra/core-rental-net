using System.Text;
using System.Text.Json;

namespace CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;

/// <summary>Every complete top-level JSON object in a streamed answer, in the order they arrived.</summary>
/// <remarks>
/// <para>
/// The answer is a <b>sequence of values, not one document</b>: one typed event per line. So this reads all of
/// them and the caller picks by what it is looking for, because an incomplete tail must be ignored rather than
/// treated as a value.
/// </para>
/// <para>
/// <b>An incomplete object stops the scan rather than throwing.</b> <c>isFinalBlock: false</c> is what makes a
/// half-arrived value a stop, and <c>AllowMultipleValues</c> is what makes the second top-level object parse at
/// all. Everything collected before the stop stands, so a reader can run this on every fragment and only ever see
/// whole objects.
/// </para>
/// </remarks>
internal static class WorkspaceSuggestionAnswerObjects
{
    public static IReadOnlyList<JsonElement> Parse(string text)
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

    public static T? Read<T>(JsonElement element)
        where T : class
    {
        try
        {
            return element.Deserialize<T>(WorkspaceSuggestionAgentJson.Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
