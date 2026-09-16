using System.Text.Json;

namespace CoreRentalNet.Host.Controllers;

/// <summary>
/// The catalogue's filter vocabulary: which words a filter accepts, and what a caller is told when it
/// sends one that is not among them.
/// </summary>
/// <remarks>
/// Shared by the binder that reads the query string and by the tests, because it is the contract
/// rather than a step in the request. An agent asks with words, so an unknown filter is refused with
/// the words that would have worked rather than answered with an empty list. Matching ignores letter
/// case and surrounding space; whether a filter may be left out is the binder's decision, not this
/// one's, because "not sent" and "not a word" are different answers.
/// </remarks>
public static class CatalogApiParameters
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

    /// <summary>The refusal: the word that was rejected, and the words that would have worked.</summary>
    public static string Refusal(string field, string? value, IReadOnlyList<string> allowed)
        => $"Unknown {field} '{value?.Trim()}'. Use one of: {string.Join(", ", allowed)}.";
}
