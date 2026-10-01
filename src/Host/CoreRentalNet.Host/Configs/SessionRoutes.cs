namespace CoreRentalNet.Host.Configs;

/// <summary>The account status route, named once.</summary>
/// <remarks>
/// Under the API prefix on purpose: that prefix is the boundary the response pipeline is scoped to, so this
/// endpoint is answered as an API - a 401 with a problem body - rather than as a page. A second copy of the
/// path is how a document comes to describe an endpoint that no longer exists.
/// </remarks>
internal static class SessionRoutes
{
    /// <summary>Whether this browser session can still produce a catalogue token.</summary>
    public const string Status = $"{CatalogRoutes.Prefix}/account/session";
}
