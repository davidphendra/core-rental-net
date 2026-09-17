using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace AgentFoundry.WorkspaceSuggestions.Catalogue;

/// <summary>
/// Reads the catalogue over HTTP, once per run, in the compact projection.
/// </summary>
/// <remarks>
/// <para>
/// One unfiltered read, reused for every slot. Filtering by category would be seven questions where one
/// answer will do, and it would put the agent's idea of what a slot means into a query string - where
/// the mapping from categories to slots already lives, in code that a test can check.
/// </para>
/// <para>
/// The wire shape is the application's own compact projection, deserialised straight into this module's
/// records rather than through a parallel set of them. They agree field for field, which is the point of
/// the contract being shared - and a translation layer between two identical shapes is where a rename
/// would go unnoticed.
/// </para>
/// </remarks>
public sealed class HttpCatalogueReader(
    HttpClient client,
    Uri catalogue,
    ICatalogueAccessToken token) : ICatalogueReader
{
    /// <summary>
    /// The catalogue's property names are camelCase and this module's are not.
    /// </summary>
    /// <remarks>
    /// The application publishes the compact view with camelCase members and members this module does not
    /// model - <c>bestFor</c> and <c>notFor</c> among them. Reading case-insensitively takes the first,
    /// and ignoring what is not modelled takes the second, so neither is a reason to keep a second copy
    /// of the shape here.
    /// </remarks>
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<CataloguePage> ReadAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, Compact())
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", await token.TokenAsync(cancellationToken).ConfigureAwait(false)) },
        };

        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new IncompleteCatalogueException(
                $"The catalogue answered {(int)response.StatusCode}, so nothing can be composed from it.");
        }

        var page = await response.Content
            .ReadFromJsonAsync<CataloguePage>(Json, cancellationToken)
            .ConfigureAwait(false);

        // A truncated page is the one thing worse than no page: it is a partial catalogue that looks
        // complete, and a slot it happens to be missing from would read as a slot the catalogue cannot
        // fill. Refused on either reading - the flag the application publishes, and the counts it
        // publishes beside it - because trusting one of two signals that can disagree is how a page
        // stops being refused the day the other changes.
        if (page is null || page.Truncated || page.Count < page.Total)
        {
            throw new IncompleteCatalogueException(
                page is null
                    ? "The catalogue answered with nothing readable."
                    : $"The catalogue sent {page.Count} of {page.Total}, so it is not all there.");
        }

        return page;
    }

    /// <summary>The compact projection of the catalogue: the thin one, which the run reuses.</summary>
    private Uri Compact() => new UriBuilder(catalogue) { Query = "view=compact" }.Uri;
}
