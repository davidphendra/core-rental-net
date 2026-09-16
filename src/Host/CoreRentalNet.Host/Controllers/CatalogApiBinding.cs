using CoreRentalNet.Modules.Catalog.Application.Queries.SearchCatalog;

namespace CoreRentalNet.Host.Controllers;

/// <summary>
/// The outcome of binding the catalogue query string: the search to answer, or the message to refuse
/// with. Exactly one of the two is set.
/// </summary>
/// <remarks>
/// A result rather than an exception, because a mis-typed filter is an ordinary outcome the endpoint
/// reports as `400`, not a fault.
/// </remarks>
public sealed record CatalogApiBinding(SearchCatalogQuery? Query, string? Error)
{
    public bool IsValid => Query is not null;

    public static CatalogApiBinding Accepted(SearchCatalogQuery query) => new(query, null);

    public static CatalogApiBinding Refused(string error) => new(null, error);
}
