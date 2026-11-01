using System.Text.Json;

using CoreRentalNet.Evaluation.Evidence.Trace;
using CoreRentalNet.Evaluation.Evidence.Context;
namespace CoreRentalNet.Evaluation.Evidence.Projection;

/// <summary>Reads one text argument from a tool-call dictionary.</summary>
public static class ToolArgumentText
{
    public static string? Read(IReadOnlyDictionary<string, object?> arguments, string name)
        => arguments.TryGetValue(name, out var value)
            ? value switch
            {
                null => null,
                string text => text,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                JsonElement { ValueKind: JsonValueKind.Null } => null,
                _ => value.ToString(),
            }
            : null;
}
