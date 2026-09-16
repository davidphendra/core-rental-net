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
}
