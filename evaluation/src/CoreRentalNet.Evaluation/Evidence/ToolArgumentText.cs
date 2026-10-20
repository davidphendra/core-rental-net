using System.Text.Json;

namespace CoreRentalNet.Evaluation.Evidence;

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
