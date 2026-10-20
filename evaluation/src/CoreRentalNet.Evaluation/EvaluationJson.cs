using System.Text.Json;

namespace CoreRentalNet.Evaluation;

/// <summary>The JSON settings every evaluation contract is read with.</summary>
public static class EvaluationJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };
}
