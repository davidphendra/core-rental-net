using System.ComponentModel;
using CoreRentalNet.Host.Configs;
using CoreRentalNet.Host.Infrastructure;
using CoreRentalNet.Host.Presentation;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Application.Queries.SearchCatalog;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;

namespace CoreRentalNet.Host.Mcp;

/// <summary>The catalogue's name search, as one MCP tool behind the catalogue's own permission.</summary>
/// <remarks>
/// <para>
/// One operation per type, as every other service here. The tool is a projection of the handler the REST
/// endpoint calls, so what it returns is what the compact REST view returns.
/// </para>
/// <para>
/// It is declared in the Host because a module may not name an MCP or ASP.NET type. The gate is the same
/// policy the REST endpoint carries, so what entitles a caller is decided once.
/// </para>
/// </remarks>
[McpServerToolType]
public sealed class SearchCatalogueTool(
    ISearchCatalogHandler handler,
    IProductCatalogService catalogue)
{
    /// <summary>The tool's name, as the model and a test both see it.</summary>
    public const string ToolName = "search_catalogue";

    /// <summary>The products whose name contains the term, capped.</summary>
    [McpServerTool(Name = ToolName, ReadOnly = true, Idempotent = true)]
    [Authorize(Policy = CatalogApiPolicy.Name)]
    [Description("Find catalogue products whose name contains a term. For a word the customer named, not for a described need.")]
    public CompactCatalogCollection Search(
        [Description("A word or phrase that appears in a product's name.")] string search,
        [Description("Narrow to one category: desk, chair or accessory. Omit for all.")] string? category = null,
        [Description("Narrow to one accessory sub-category: beanbag, coffee, lamp, monitor or plant. Omit for all.")] string? subCategory = null,
        [Description("The most products to return.")] int limit = CatalogToolLimits.Default)
    {
        CatalogToolFilters.CheckTerm(search, "search");

        var matched = handler.Handle(new SearchCatalogQuery(
            CatalogToolFilters.Category(category),
            CatalogToolFilters.SubCategory(subCategory),
            search));

        return CatalogAnswer.Compact(matched, CatalogToolLimits.Clamp(limit), catalogue.All);
    }
}
