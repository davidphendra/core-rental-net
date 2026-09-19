namespace CoreRentalNet.Host.Controllers;

/// <summary>
/// The route the workspace builder publishes, named once.
/// </summary>
/// <remarks>
/// Beside <see cref="CatalogRoutes"/>, and composed from the same prefix, because that prefix is the
/// boundary the response pipeline is scoped to: everything under it is answered as an API, with the one
/// problem-details shape, and everything else is answered as a page. A route written out in full would
/// look the same and answer page-shaped refusals the day somebody changed the prefix, so the prefix is
/// referenced rather than repeated.
/// </remarks>
internal static class BuilderRoutes
{
    /// <summary>Runs one suggestion and streams what happens while it runs.</summary>
    public const string Suggest = $"{CatalogRoutes.Prefix}/builder/suggest";
}
