using System.Text.Json;
using CoreRentalNet.CatalogPopulate.Compact;

namespace CoreRentalNet.CatalogPopulate.Transform;

/// <summary>Reads one string out of a source row's <c>details</c>, or the unspecified marker.</summary>
internal static class DetailReader
{
    /// <summary>The text of the named detail, or <see cref="AttributeNames.Unspecified"/>.</summary>
    public static string Text(IReadOnlyDictionary<string, JsonElement> details, string key)
    {
        if (!details.TryGetValue(key, out var value))
        {
            return AttributeNames.Unspecified;
        }

        var text = value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();

        return string.IsNullOrWhiteSpace(text) ? AttributeNames.Unspecified : text.Trim();
    }

    /// <summary>The first of the named details that the source actually carries.</summary>
    public static string First(IReadOnlyDictionary<string, JsonElement> details, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = Text(details, key);
            if (value != AttributeNames.Unspecified)
            {
                return value;
            }
        }

        return AttributeNames.Unspecified;
    }
}
