using System.Security.Claims;
using CoreRentalNet.Host.Infrastructure;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Application.Queries.SearchCatalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreRentalNet.Host.Controllers;

/// <summary>
/// The catalogue's read endpoint: the one operation a machine caller asks for.
/// </summary>
/// <remarks>
/// A controller, so each of the endpoint's rules is declared where the operation is rather than
/// attached from outside: the gate is an attribute, the query string is bound by the framework, the
/// answers are documented by attributes, and a filter that is not one of the catalogue's words is
/// refused by <c>[ApiController]</c> with a problem-details <c>400</c> before the action runs. The
/// vocabulary those refusals name lives in <see cref="CatalogApiParameters"/>.
/// </remarks>
[ApiController]
[Route(CatalogRoutes.Catalogue)]
public sealed class CatalogController(
    ISearchCatalogHandler catalog,
    ILoggerFactory loggers) : ControllerBase
{
    /// <summary>Lists the catalogue, optionally narrowed by category, subcategory and product name.</summary>
    /// <remarks>
    /// Every filter is optional, and none of them returns the whole catalogue. A term matches a
    /// product's name only. An unknown category or subcategory is refused with the values that would
    /// have worked rather than answered with an empty list, and no match is an empty list rather than a
    /// 404: "there are none of those" and "there is no such endpoint" are different answers.
    /// </remarks>
    [HttpGet]
    [Authorize(Policy = CatalogApiPolicy.Name)]
    [ProducesResponseType<IReadOnlyList<ProductView>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<IReadOnlyList<ProductView>> Search(
        [FromQuery] CatalogCategory? category,
        [FromQuery] CatalogSubCategory? subCategory,
        [FromQuery] string? search)
    {
        var products = catalog.Handle(new SearchCatalogQuery(category, subCategory, Blank(search)));

        // Written after authorisation and after the answer: a refused call never reaches this line, and
        // the count is what actually came back rather than what was asked for. The filters are logged
        // as the caller wrote them, not as they were parsed.
        CatalogApiLog.Called(
            loggers.CreateLogger(CatalogApiLog.Category),
            Caller(),
            AsAsked("category"),
            AsAsked("subCategory"),
            AsAsked("search"),
            products.Count);

        return Ok(products);
    }

    /// <summary>A search of nothing but spaces narrows nothing, so it is not a filter.</summary>
    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    /// <summary>The filter as the request wrote it, for the line this endpoint logs.</summary>
    private string? AsAsked(string name) => Request.Query[name].FirstOrDefault();

    /// <summary>Who made the request, as the provider states it: the subject, or the client id.</summary>
    private string Caller()
        => User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? "anonymous";
}
