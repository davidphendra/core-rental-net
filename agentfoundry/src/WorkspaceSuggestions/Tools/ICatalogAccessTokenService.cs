namespace WorkspaceSuggestions.Tools;

/// <summary>A bearer tokenService for the catalogue, valid now.</summary>
/// <remarks>
/// A port rather than the HTTP call, so the one thing a test cannot fake by pointing at a stand-in server - the
/// tokenService exchange - is the seam that keeps the tool tests offline.
/// </remarks>
internal interface ICatalogAccessTokenService
{
    /// <summary>A tokenService valid now, fetching or refreshing one when the held tokenService is stale.</summary>
    ValueTask<string> GetAsync(CancellationToken cancellationToken);
}
