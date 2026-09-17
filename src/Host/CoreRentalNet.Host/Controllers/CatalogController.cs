using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Host.Infrastructure;
using CoreRentalNet.Host.Presentation;
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
    IProductCatalog everyProduct,
    ILoggerFactory loggers) : ControllerBase
{
    /// <summary>The content types the API actually answers with, named rather than left to the formatters.</summary>
    /// <remarks>
    /// Without these the document lists every output formatter the controller has - <c>text/plain</c>
    /// and <c>text/json</c> beside <c>application/json</c> - so a generated client is told to expect
    /// three shapes where the endpoint sends one, and told <c>application/json</c> for a refusal that
    /// actually arrives as <c>application/problem+json</c>.
    /// </remarks>
    private const string Json = "application/json";

    private const string ProblemJson = "application/problem+json";

    /// <summary>Lists the catalogue, optionally narrowed by category, subcategory and product name.</summary>
    /// <remarks>
    /// Every filter is optional, and none of them returns the whole catalogue. A term matches a
    /// product's name only. An unknown category or subcategory is refused with the values that would
    /// have worked rather than answered with an empty list, and no match is an empty list rather than a
    /// 404: "there are none of those" and "there is no such endpoint" are different answers.
    ///
    /// <c>view</c> chooses how much of each product comes back. <c>full</c> is every field the module
    /// publishes and is what a caller that says nothing gets; <c>compact</c> leaves out the image path
    /// and the display flags, states the price as a number and the currency once on the envelope, and
    /// keeps the description and the metadata - the fields a request is matched against.
    ///
    /// <c>limit</c> caps how many rows come back. It is not paging: a caller that receives fewer rows
    /// than the answer says matched is told so, rather than left to assume it has all of them.
    /// </remarks>
    [HttpGet]
    [Authorize(Policy = CatalogApiPolicy.Name)]
    [ProducesResponseType<ApiCollection<ProductView>>(StatusCodes.Status200OK, Json)]
    // The compact projection is described in the remarks above rather than declared here as a second
    // 200: a status code has one schema per content type, so a second declaration replaces the first
    // and the document would describe the projection while claiming it is the default.
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemJson)]
    // Declared here rather than added to every operation by a transformer: the API has one operation,
    // and a reader looking for what this route can answer should find it on the route. These carry the
    // schema a generated client needs, which a transformer could only add by assuming one exists.
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status405MethodNotAllowed, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, ProblemJson)]
    public ActionResult Search(
        [FromQuery] CatalogCategory? category,
        [FromQuery] CatalogSubCategory? subCategory,
        [FromQuery] string? search,
        [FromQuery] CatalogProjection? view,
        [FromQuery, Range(1, int.MaxValue)] int? limit)
    {
        var matched = catalog.Handle(new SearchCatalogQuery(category, subCategory, Blank(search)));
        var page = Cap(matched, limit ?? CatalogApiLimits.Default);

        // Written after authorisation and after the answer: a refused call never reaches this line, and
        // the count is what actually came back rather than what was asked for. The filters are logged
        // as the caller wrote them, not as they were parsed.
        CatalogApiLog.Called(
            loggers.CreateLogger(CatalogApiLog.Category),
            Caller(),
            AsAsked("category"),
            AsAsked("subCategory"),
            AsAsked("search"),
            page.Count);

        // A collection answer, not a bare array: the count travels with the items, and a page of them
        // can be added later without breaking the callers who read this shape today.
        return (view ?? CatalogProjection.Full) == CatalogProjection.Compact
            ? Ok(CompactCatalogProjection.Of(page, matched.Count, CatalogueCurrency()))
            : Ok(new ApiCollection<ProductView>(page, page.Count, matched.Count));
    }

    /// <summary>The rows this answer carries: everything that matched, or the first <paramref name="limit"/> of them.</summary>
    /// <remarks>
    /// Counted here rather than by the handler, because "how many matched" and "how many travel" are
    /// the two numbers the envelope has to keep apart - and only this layer knows the cap.
    /// </remarks>
    private static IReadOnlyList<ProductView> Cap(IReadOnlyList<ProductView> matched, int limit)
        => matched.Count <= limit ? matched : [.. matched.Take(limit)];

    /// <summary>The currency the catalogue is priced in, which the compact envelope states once.</summary>
    /// <remarks>
    /// Read from the catalogue rather than written here, and from the whole of it rather than from the
    /// rows that matched: an empty result still has a currency, and a filtered result must not decide
    /// what it is. The loader refuses an empty file and refuses a row priced in anything but the
    /// settlement currency, so the fallback is unreachable - it is there so that a broken invariant is
    /// answered rather than thrown at a caller.
    /// </remarks>
    private string CatalogueCurrency()
    {
        var catalogue = everyProduct.All;

        return catalogue.Count == 0 ? Currencies.Idr : catalogue[0].MonthlyPrice.Currency;
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
