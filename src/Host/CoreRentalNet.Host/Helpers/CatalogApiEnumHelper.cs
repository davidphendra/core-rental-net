using System.Text.Json;

namespace CoreRentalNet.Host.Helper;

public static class CatalogApiEnumHelper
{
    /// <summary>Does this text name one of the enum's members, ignoring case and surrounding space?</summary>
    /// <remarks>
    /// A number is not a name and matches nothing: the wire vocabulary is the words, and accepting an
    /// ordinal would let a caller select a category that a reordering would silently change. Absent
    /// text names nothing either.
    /// </remarks>
    public static bool TryMatch<TEnum>(string? value, out TEnum matched)
        where TEnum : struct, Enum
    {
        matched = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();

        foreach (var name in Enum.GetNames<TEnum>())
        {
            if (string.Equals(name, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                matched = Enum.Parse<TEnum>(name);

                return true;
            }
        }

        return false;
    }

    /// <summary>The vocabulary as the wire writes it, so a refusal names the words the caller may send.</summary>
    public static string[] Names<TEnum>()
        where TEnum : struct, Enum
        => [.. Enum.GetNames<TEnum>().Select(JsonNamingPolicy.CamelCase.ConvertName)];
}