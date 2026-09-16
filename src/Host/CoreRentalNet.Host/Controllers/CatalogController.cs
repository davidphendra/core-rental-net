using CoreRentalNet.Host.Infrastructure;
using CoreRentalNet.Modules.Catalog.Application.Queries.SearchCatalog;

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
            string? category,
            string? subCategory,
            string? search,
            ISearchCatalogHandler catalog) =>
        {
            var binding = CatalogApiParameters.Bind(category, subCategory, search);

            return binding.Query is null
                ? Results.BadRequest(binding.Error)
                : Results.Ok(catalog.Handle(binding.Query));
        }).RequireAuthorization(CatalogApiPolicy.Name);
    }
}
