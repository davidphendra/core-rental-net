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
    public async Task<CompactCatalogCollection> Search(
        [Description("A sentence describing what is wanted.")] string query,
        [Description("Narrow to one category: desk, chair or accessory. Omit for all.")] string? category = null,
        [Description("Narrow to one accessory sub-category: beanbag, coffee, lamp, monitor or plant. Omit for all.")] string? subCategory = null,
        [Description("The most products to return.")] int limit = CatalogToolLimits.Default,
        CancellationToken cancellationToken = default)
    {
        CatalogToolFilters.CheckTerm(query, "query");

        try
        {
            var matched = await handler.HandleAsync(
                new SearchSimilarityCatalogQuery(
                    CatalogToolFilters.Category(category),
                    CatalogToolFilters.SubCategory(subCategory),
                    query),
                cancellationToken).ConfigureAwait(false);

            return CatalogAnswer.Compact(matched, CatalogToolLimits.Clamp(limit), catalogue.All);
        }
        catch (ProductSimilarityUnavailableException exception)
        {
            throw CatalogToolFailure.Refuse(exception, logger, ToolName);
        }
    }
}
