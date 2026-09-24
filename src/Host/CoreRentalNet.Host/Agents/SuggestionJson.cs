using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoreRentalNet.Host.Agents;

/// <summary>The one serializer configuration for talking to the agent.</summary>
/// <remarks>
/// <para>
/// Enums are camel case because that is what the contract asks for — <c>"desk"</c>, <c>"suggested"</c>,
/// <c>"notWorkspace"</c> — and the application already writes its own API that way, so this is the same
/// convention rather than a second one.
/// </para>
/// <para>
/// The exception is the slot vocabulary, which the contract declares PascalCase (<c>"Desk"</c>). That is
/// attached to the two properties that carry a slot, in <c>AgentSuggestionLine</c> and
/// <c>SuggestionSlotRule</c>, rather than added here — adding it here would rename every enum in the payload.
/// </para>
/// </remarks>
internal static class SuggestionJson
{
    public static JsonSerializerOptions Options { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        return options;
    }
}
