using System.Text.Json;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Application.Queries.SearchCatalog;

namespace CoreRentalNet.Host.Controllers;

/// <summary>
/// Binds the catalogue endpoint's query string to a search.
/// </summary>
/// <remarks>
/// An agent asks with words, so an unknown filter is refused with the words that would have worked
/// rather than answered with an empty list. Matching ignores letter case and surrounding space; a bare
/// number is not a name and is refused with everything else. A blank filter narrows nothing.
/// </remarks>
public static class CatalogApiParameters
{
    public static CatalogApiBinding Bind(string? category, string? subCategory, string? search)
    {
        if (!TryMatch<CatalogCategory>(category, out var parsedCategory))
        {
            return CatalogApiBinding.Refused(Refusal("category", category, Names<CatalogCategory>()));
        }

        if (!TryMatch<CatalogSubCategory>(subCategory, out var parsedSubCategory))
        {
            return CatalogApiBinding.Refused(Refusal("subCategory", subCategory, Names<CatalogSubCategory>()));
        }

        return CatalogApiBinding.Accepted(new SearchCatalogQuery(
            Absent(category) ? null : parsedCategory,
            Absent(subCategory) ? null : parsedSubCategory,
            string.IsNullOrWhiteSpace(search) ? null : search));
    }

    /// <summary>Matches one of the enum's names, ignoring case; a number or a stranger matches nothing.</summary>
    private static bool TryMatch<TEnum>(string? value, out TEnum matched)
        where TEnum : struct, Enum
    {
        matched = default;

        if (Absent(value))
        {
            return true;
        }

        var trimmed = value!.Trim();

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

    private static bool Absent(string? value) => string.IsNullOrWhiteSpace(value);

    /// <summary>The vocabulary as the wire writes it, so the refusal names the words the caller may send.</summary>
    private static string[] Names<TEnum>()
        where TEnum : struct, Enum
        => [.. Enum.GetNames<TEnum>().Select(JsonNamingPolicy.CamelCase.ConvertName)];

    private static string Refusal(string field, string? value, IReadOnlyList<string> allowed)
        => $"Unknown {field} '{value?.Trim()}'. Use one of: {string.Join(", ", allowed)}.";
}
