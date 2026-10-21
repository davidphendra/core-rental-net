using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoreRentalNet.Evaluation;

/// <summary>The JSON settings every evaluation contract is read with.</summary>
public static class EvaluationJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };
}
