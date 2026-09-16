namespace CoreRentalNet.Host.Controllers;

/// <summary>
/// The routes the API publishes, named once.
/// </summary>
/// <remarks>
/// The catalogue's path is referenced by the controller that maps it and by the document transformer
/// that describes it. Two copies of a route string is how a document comes to describe an endpoint
/// that does not exist, or to miss one that does.
/// </remarks>
internal static class CatalogRoutes
{
    /// <summary>The catalogue endpoint: the one operation a machine caller asks for.</summary>
    public const string Catalogue = "/api/catalog";
}
