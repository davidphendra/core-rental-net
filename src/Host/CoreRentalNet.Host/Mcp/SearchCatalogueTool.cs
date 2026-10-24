using System.ComponentModel;
using System.Diagnostics;
using CoreRentalNet.Host.Configs;
using CoreRentalNet.Host.Infrastructure;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Application.Queries.SearchCatalog;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;

namespace CoreRentalNet.Host.Mcp;

/// <summary>The catalogue's name search, as one MCP tool behind the catalogue's own permission.</summary>
/// <remarks>
/// <para>
/// One operation per type, as every other service here. The tool is a projection of the handler the REST
/// endpoint calls, so what it returns is what the REST endpoint returns.
/// </para>
/// <para>
/// The product projection is the MCP read model rather than the REST DTO. Its descriptions are bounded by
/// <see cref="CatalogueDescriptionPolicy"/>, so a model receives only the text the tool boundary allows.
/// </para>
/// <para>
/// <b>Several terms rather than one word, because that is what the stage before it produces.</b> A search that
/// has been expanded into terms has already decided what is wanted, and one term per call would make it ask
/// the same question several times and rank the answers itself. The terms are alternatives: every word of one
/// term must appear, and a product needs to match only one term.
/// </para>
/// <para>
/// <b>The ceiling is applied by the catalogue's own narrowing, not by this tool.</b> Both searches narrow
/// through the same operation, so the name search and the meaning search cannot disagree about what is
/// eligible.
/// </para>
/// <para>
/// It is declared in the Host because a module may not name an MCP or ASP.NET type. The gate is the same
/// policy the REST endpoint carries, so what entitles a caller is decided once.
/// </para>
/// </remarks>
[McpServerToolType]
public sealed class SearchCatalogueTool(
    ISearchCatalogHandler handler,
    IProductCatalogService catalogue,
    HostRequestTiming timing)
{
    /// <summary>The tool's name, as the model and a test both see it.</summary>
    public const string ToolName = "search_catalogue";

    /// <summary>The products matching any of the terms, capped, with what a monthly ceiling excluded.</summary>
    [McpServerTool(Name = ToolName, ReadOnly = true, Idempotent = true)]
    [Authorize(Policy = CatalogApiPolicy.Name)]
    [Description("Find the catalogue products whose name best matches any of several terms, best match first. For the terms a request was expanded into, not for a sentence describing a need.")]
    public CatalogueSearchToolAnswer Search(
        [Description("One or more terms or short phrases that appear in a product's name. A product needs to match only one of them.")] string[] terms,
        [Description("The category to search: desk, chair or accessory. Required. A bean bag is category accessory with subCategory beanbag; there is no relaxzone category.")] string category,
        [Description("Narrow to one accessory sub-category: beanbag, coffee, lamp, monitor or plant. Required for accessories, omitted for desks and chairs.")] string? subCategory = null,
        [Description("The most a product may cost each month. A product above it is never returned. Omit for no ceiling.")] decimal? maximumMonthlyAmount = null,
        [Description("The most products to return.")] int limit = CatalogToolLimits.DefaultProductCount)
    {
        CatalogToolFilters.CheckSearchTerms(terms);

        var scope = CatalogToolFilters.ScopeFrom(category, subCategory);
        var take = CatalogToolLimits.Clamp(limit);

        var stopwatch = Stopwatch.StartNew();
        var matched = handler.Handle(new SearchCatalogQuery(
            scope,
            ExpandedSearchTerms: terms,
            MaximumMonthlyAmount: maximumMonthlyAmount,
            Take: take));
        timing.Record("name-search", stopwatch.Elapsed);

        return CatalogueSearchToolAnswer.From(
            matched,
            take,
            CheapestProductExcludedByTheCeiling(scope, matched, maximumMonthlyAmount));
    }

    /// <summary>The cheapest product a monthly ceiling excluded, or null when it excluded nothing.</summary>
    /// <remarks>
    /// <b>A search that returned nothing because of the ceiling and one that returned nothing because the
    /// catalogue has none look the same in an answer, and only the first is the caller's to fix.</b> The number
    /// is the whole of what the next attempt needs to know, and it costs one pass over the snapshot that is
    /// already in memory — spent only when the answer is empty, which is the only time it can be wanted.
    /// </remarks>
    private decimal? CheapestProductExcludedByTheCeiling(
        CatalogScope scope,
        IReadOnlyList<ProductDto> matched,
        decimal? maximumMonthlyAmount)
    {
        if (matched.Count > 0 || maximumMonthlyAmount is null)
        {
            return null;
        }

        var productsIgnoringTheCeiling = catalogue
            .SearchPageAsync(scope, null, 0, CatalogPageLimits.Maximum, CancellationToken.None)
            .GetAwaiter()
            .GetResult()
            .Items;

        return productsIgnoringTheCeiling.Count == 0
            ? null
            : productsIgnoringTheCeiling.Min(product => product.MonthlyPrice.Amount);
    }
}
