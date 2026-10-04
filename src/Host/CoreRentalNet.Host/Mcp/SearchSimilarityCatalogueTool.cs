using System.ComponentModel;
using CoreRentalNet.Host.Configs;
using CoreRentalNet.Host.Infrastructure;
using CoreRentalNet.Host.Presentation;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Application.Queries.SearchSimilarityCatalog;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;

namespace CoreRentalNet.Host.Mcp;

/// <summary>The catalogue's meaning search, as one MCP tool behind its own permission.</summary>
/// <remarks>
/// <para>
/// A different permission, a different handler and a different failure mode from the name search, which is why
/// it is a type of its own rather than a second method: what is gated and what is offered stay one list.
/// </para>
/// <para>
/// A failure to search is answered as a failure, never as the name search's answer. Turning "we could not
/// retrieve" into "here are some name matches" is the failure nobody reports.
/// </para>
/// <para>
/// <b>It takes a sentence and one ceiling, and it narrows exactly as the name search does.</b> Both hand the
/// ceiling to the same catalogue operation, so a product above what a caller may spend is ineligible to
/// either search rather than ineligible to one of them.
/// </para>
/// </remarks>
[McpServerToolType]
public sealed class SearchSimilarityCatalogueTool(
    ISearchSimilarityCatalogHandler handler,
    IProductCatalogService catalogue,
    ILogger<SearchSimilarityCatalogueTool> logger)
{
    /// <summary>The tool's name, as the model and a test both see it.</summary>
    public const string ToolName = "search_similarity_catalogue";

    /// <summary>The products nearest the sentence in meaning, capped, nearest first.</summary>
    [McpServerTool(Name = ToolName, ReadOnly = true, Idempotent = true)]
    [Authorize(Policy = SimilaritySearchPolicy.Name)]
    [Description("Find the catalogue products nearest a sentence in meaning. For a described need or a purpose.")]
    public async Task<CatalogueSearchToolAnswer> Search(
        [Description("A sentence describing what is wanted.")] string query,
        [Description("Narrow to one category: desk, chair or accessory. Omit for all.")] string? category = null,
        [Description("Narrow to one accessory sub-category: beanbag, coffee, lamp, monitor or plant. Omit for all.")] string? subCategory = null,
        [Description("The most a product may cost each month. A product above it is never returned. Omit for no ceiling.")] decimal? maximumMonthlyAmount = null,
        [Description("The most products to return.")] int limit = CatalogToolLimits.DefaultProductCount,
        CancellationToken cancellationToken = default)
    {
        CatalogToolFilters.CheckSearchText(query);

        var parsedCategory = CatalogToolFilters.CatalogueCategoryFrom(category);
        var parsedSubCategory = CatalogToolFilters.CatalogueSubCategoryFrom(subCategory);

        try
        {
            var matched = await handler.HandleAsync(
                new SearchSimilarityCatalogQuery(
                    parsedCategory,
                    parsedSubCategory,
                    query,
                    maximumMonthlyAmount),
                cancellationToken).ConfigureAwait(false);

            return new CatalogueSearchToolAnswer(
                CatalogAnswer.CompactForTools(
                    matched,
                    CatalogToolLimits.Clamp(limit),
                    catalogue.All,
                    CatalogToolLimits.MaximumDescriptionCharacterCount),
                CheapestProductExcludedByTheCeiling(matched, parsedCategory, parsedSubCategory, maximumMonthlyAmount));
        }
        catch (ProductSimilarityUnavailableException exception)
        {
            throw CatalogToolFailure.Refuse(exception, logger, ToolName);
        }
    }

    /// <summary>The cheapest product a monthly ceiling excluded, or null when it excluded nothing.</summary>
    /// <remarks>
    /// The same answer the name search gives, for the same reason and by the same rule: an empty answer caused
    /// by the ceiling and an empty answer caused by the catalogue are the same absence and different problems.
    /// </remarks>
    private decimal? CheapestProductExcludedByTheCeiling(
        IReadOnlyList<ProductView> matched,
        CatalogCategory? category,
        CatalogSubCategory? subCategory,
        decimal? maximumMonthlyAmount)
    {
        if (matched.Count > 0 || maximumMonthlyAmount is null)
        {
            return null;
        }

        var productsIgnoringTheCeiling = catalogue.Search(category, subCategory);

        return productsIgnoringTheCeiling.Count == 0
            ? null
            : productsIgnoringTheCeiling.Min(product => product.MonthlyPrice.Amount);
    }
}
