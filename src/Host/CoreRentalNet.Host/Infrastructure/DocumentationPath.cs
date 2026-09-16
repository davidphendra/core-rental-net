namespace CoreRentalNet.Host.Infrastructure;

/// <summary>
/// Where the development documentation is served, named once.
/// </summary>
/// <remarks>
/// This path is a security boundary as well as a route: the security headers relax their policy for
/// it, so the route and the exception are one constant rather than two strings that agree until they
/// do not.
/// </remarks>
internal static class DocumentationPath
{
    /// <summary>The route prefix, without a leading slash, as the UI's options want it.</summary>
    public const string Prefix = "swagger";

    /// <summary>The same path as a request path, as the middleware matches it.</summary>
    public const string RequestPath = "/swagger";

    /// <summary>
    /// The document the application publishes, and the address the page must be pointed at.
    /// </summary>
    /// <remarks>
    /// Swashbuckle's page defaults to <c>v1/swagger.json</c>, which this application does not serve -
    /// its document comes from <c>MapOpenApi</c>, at this address. Left unset, the page loads and shows
    /// no operations at all, which looks like a working page until somebody reads it. Named once here
    /// and used by both, so the two cannot disagree.
    /// </remarks>
    public const string Document = "/openapi/v1.json";
}
