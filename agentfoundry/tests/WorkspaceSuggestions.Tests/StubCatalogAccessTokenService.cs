using WorkspaceSuggestions.Tools;

namespace WorkspaceSuggestions.Tests;

/// <summary>A token provider that answers without a token endpoint.</summary>
/// <remarks>
/// Used where the token is not what is under test - the catalogue source's unconfigured path, which returns
/// before it would ever ask for one.
/// </remarks>
internal sealed class StubCatalogAccessToken : ICatalogAccessToken
{
    public ValueTask<string> GetAsync(CancellationToken cancellationToken) => ValueTask.FromResult("token");
}
