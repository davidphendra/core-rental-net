using System.Security.Claims;
using CoreRentalNet.Host.Infrastructure;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Application.Queries.SearchCatalog;
using Microsoft.Extensions.Logging;

namespace CoreRentalNet.Host.Controllers;

/// <summary>
/// The catalogue's read endpoint: the one operation a machine caller asks for.
/// </summary>
/// <remarks>
/// Endpoint mappings rather than an MVC controller, for the reason
/// <see cref="AccountController"/> gives: the application has no model binding or filter pipeline, and
/// a single read route does not justify bringing one in.
/// </remarks>
public static class CatalogController
{
    /// <summary>Maps <c>GET /api/catalog</c>, the catalogue narrowed by what the caller asked for.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/api/catalog", (
            HttpContext context,
            string? category,
            string? subCategory,
            string? search,
            ISearchCatalogHandler catalog,
            ILoggerFactory loggers) =>
        {
            var binding = CatalogApiParameters.Bind(category, subCategory, search);

            if (binding.Query is null)
            {
                return Results.BadRequest(binding.Error);
            }

            var products = catalog.Handle(binding.Query);

            // After authorisation and after the answer: a refused call never reaches this line, and
            // the count is what actually came back rather than what was asked for.
            CatalogApiLog.Called(loggers.CreateLogger(CatalogApiLog.Category), Caller(context), category, subCategory, search, products.Count);

            return Results.Ok(products);
        })
            .WithName("SearchCatalog")
            .WithSummary("Lists the catalogue, optionally narrowed by category, subcategory and name.")
            .WithDescription(
                "Every filter is optional, and none of them returns the whole catalogue. A term matches "
                + "a product's name only. An unknown category or subcategory is refused with the values "
                + "that would have worked rather than answered with an empty list.")
            .WithTags("Catalog")
            .Produces<IReadOnlyList<ProductView>>(StatusCodes.Status200OK)
            .Produces<string>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization(CatalogApiPolicy.Name);
    }

    /// <summary>Who made the request, as the provider states it: the subject, or the client id.</summary>
    private static string Caller(HttpContext context)
        => context.User.FindFirst("sub")?.Value
            ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? "anonymous";
}
