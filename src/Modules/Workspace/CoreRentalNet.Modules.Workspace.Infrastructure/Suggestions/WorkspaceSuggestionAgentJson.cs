using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;

/// <summary>The one serializer configuration for talking to the agent.</summary>
/// <remarks>
/// Enums are camel case because that is what the contract asks for - <c>"desk"</c>, <c>"suggested"</c>,
/// <c>"notWorkspace"</c>. The exception is the slot vocabulary, which the contract declares PascalCase; that
/// converter is attached to the two properties that carry a slot rather than added here.
/// </remarks>
internal static class WorkspaceSuggestionAgentJson
{
    public static JsonSerializerOptions Options { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        return options;
    }
}
