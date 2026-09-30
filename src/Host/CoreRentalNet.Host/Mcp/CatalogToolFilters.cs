using CoreRentalNet.Host.Configs;
using CoreRentalNet.Host.Helper;
using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Host.Mcp;

/// <summary>Turns the words a model sends into the catalogue's vocabulary and its boundaries, or refuses them.</summary>
/// <remarks>
/// <para>
/// <b>The MCP path does not pass through <c>[ApiController]</c> model-state validation</b>, so the refusal the
/// REST binder would produce is produced here instead. The vocabulary and the refusal come from the same
/// helpers the REST endpoint uses, so the two surfaces cannot disagree about which words are the catalogue's.
/// </para>
/// <para>
/// <b>A word that is not one of them is refused, never defaulted.</b> A model that sent "sofa" and was quietly
/// given every category would be told its filter worked; the REST API already refuses that, and this is the
/// same answer through a different door.
/// </para>
/// <para>
/// <b>And a list that is too long is refused rather than trimmed.</b> A model that sent nine terms and was
/// searched for eight would be told its search worked — and it is the model that would carry that belief into
/// the next attempt.
/// </para>
/// </remarks>
internal static class CatalogToolFilters
{
    /// <summary>The category the caller named, or null when it named none.</summary>
    public static CatalogCategory? CatalogueCategoryFrom(string? value) => Parse<CatalogCategory>(value, "category");

    /// <summary>The accessory subcategory the caller named, or null when it named none.</summary>
    public static CatalogSubCategory? CatalogueSubCategoryFrom(string? value)
        => Parse<CatalogSubCategory>(value, "subCategory");

    /// <summary>Refuses a sentence that is blank or longer than the boundary allows.</summary>
    public static void CheckSearchText(string? query)
        => CheckLength(query, "query", CatalogToolLimits.MaximumSearchTextCharacterCount);

    /// <summary>Refuses a term that is blank or longer than the boundary allows.</summary>
    public static void CheckSearchTerm(string? searchTerm)
        => CheckLength(searchTerm, "terms", CatalogToolLimits.MaximumSearchTermCharacterCount);

    /// <summary>Refuses an empty, oversized or over-long term list.</summary>
    /// <remarks>
    /// <para>
    /// <b>Refused, never trimmed, because a search that quietly dropped the ninth term would answer as though
    /// all nine had been asked for.</b> The bound is a boundary, not a lesson: measured, the MCP server answers
    /// a thrown tool with a generic line of its own, so the sentence below reaches the log and not the model.
    /// </para>
    /// <para>
    /// <b>What keeps the model inside the bound is the prompt and the agent's own policy, not this.</b> The
    /// agent bounds a component's words before they are ever sent, so this refusal is unreachable from this
    /// application's own pipeline and exists for everything else that can call the tool.
    /// </para>
    /// </remarks>
    public static void CheckSearchTerms(IReadOnlyList<string>? searchTerms)
    {
        ArgumentNullException.ThrowIfNull(searchTerms);

        if (searchTerms.Count == 0)
        {
            throw new ArgumentException("A catalogue search needs at least one term.", nameof(searchTerms));
        }

        if (searchTerms.Count > CatalogToolLimits.MaximumSearchTermCount)
        {
            throw new ArgumentException(
                $"A catalogue search accepts at most {CatalogToolLimits.MaximumSearchTermCount} terms.",
                nameof(searchTerms));
        }

        foreach (var searchTerm in searchTerms)
        {
            CheckSearchTerm(searchTerm);
        }
    }

    /// <summary>Refuses a value that is blank, or longer than the boundary for its field allows.</summary>
    private static void CheckLength(string? value, string field, int maximumLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (value.Length > maximumLength)
        {
            throw new ArgumentException($"A {field} is at most {maximumLength} characters.", field);
        }
    }

    private static TEnum? Parse<TEnum>(string? value, string field)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (CatalogApiEnumHelper.TryMatch<TEnum>(value, out var matched))
        {
            return matched;
        }

        throw new ArgumentException(
            CatalogApiParameterHelper.Refusal(field, value, [.. CatalogApiEnumHelper.Names<TEnum>()]),
            field);
    }
}
