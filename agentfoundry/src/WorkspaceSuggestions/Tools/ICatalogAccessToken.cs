namespace WorkspaceSuggestions.Tools;

/// <summary>A bearer token for the catalogue, valid now.</summary>
/// <remarks>
/// A port rather than the HTTP call, so the one thing a test cannot fake by pointing at a stand-in server - the
/// token exchange - is the seam that keeps the tool tests offline.
/// </remarks>
internal interface ICatalogAccessToken
{
    /// <summary>A token valid now, fetching or refreshing one when the held token is stale.</summary>
    ValueTask<string> GetAsync(CancellationToken cancellationToken);
}
