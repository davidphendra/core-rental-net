using System.Text.Json;
using System.Text.Json.Serialization;

namespace WorkspaceSuggestions.Contracts;

/// <summary>The one serializer configuration for the contracts, on the wire and in the run record.</summary>
/// <remarks>
/// The same conventions the schemas declare: camel-case names from <c>JsonPropertyName</c> or from the
/// property itself, and enums as the words the schema lists rather than as numbers. Having one place means a
/// report the code writes and an answer the model writes cannot end up spelled two ways.
/// </remarks>
internal static class ContractJson
{
    public static JsonSerializerOptions Options { get; } = Create();

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        return options;
    }
}
