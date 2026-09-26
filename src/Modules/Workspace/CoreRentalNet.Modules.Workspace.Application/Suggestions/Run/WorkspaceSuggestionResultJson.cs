using System.Text.Json;
using System.Text.Json.Serialization;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;

/// <summary>The one serializer configuration for a run's terminal frame, shared by the writer and the page.</summary>
/// <remarks>
/// Enums are camel case because that is what the wire asks for, and the page reads the frame back with the
/// same configuration, so the writer and the reader can never drift.
/// </remarks>
public static class WorkspaceSuggestionResultJson
{
    public static JsonSerializerOptions Options { get; } = Create();

    public static string Serialize(WorkspaceSuggestionResultFrame resultFrame)
        => JsonSerializer.Serialize(resultFrame, Options);

    public static WorkspaceSuggestionResultFrame? Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<WorkspaceSuggestionResultFrame>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        return options;
    }
}
