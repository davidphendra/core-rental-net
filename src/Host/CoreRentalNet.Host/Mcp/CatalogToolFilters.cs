using CoreRentalNet.Host.Configs;
using CoreRentalNet.Host.Helper;
using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Host.Mcp;

/// <summary>Turns the words a model sends into the catalogue's vocabulary, or refuses them.</summary>
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
/// </remarks>
internal static class CatalogToolFilters
{
    /// <summary>The category the model named, or null when it named none.</summary>
    public static CatalogCategory? Category(string? value) => Parse<CatalogCategory>(value, "category");

    /// <summary>The accessory sub-category the model named, or null when it named none.</summary>
    public static CatalogSubCategory? SubCategory(string? value) => Parse<CatalogSubCategory>(value, "subCategory");

    /// <summary>Refuses a blank term or one longer than the boundary allows.</summary>
    public static void CheckTerm(string? value, string field)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (value.Length > CatalogToolLimits.MaxQueryLength)
        {
            throw new ArgumentException(
                $"A {field} is at most {CatalogToolLimits.MaxQueryLength} characters.",
                field);
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
