using WorkspaceSuggestions.Tools;

namespace WorkspaceSuggestions.Tests;

/// <summary>A tokenService provider that answers without a tokenService endpoint.</summary>
/// <remarks>
/// Used where the tokenService is not what is under test - the catalogue source's unconfigured path, which returns
/// before it would ever ask for one.
/// </remarks>
internal sealed class StubCatalogAccessTokenService : ICatalogAccessTokenService
{
    public ValueTask<string> GetAsync(CancellationToken cancellationToken) => ValueTask.FromResult("tokenService");
}
