using System.Net.Http.Headers;

namespace WorkspaceSuggestions.Tools;

/// <summary>Attaches the catalogue's bearer token to every MCP request, fresh each time.</summary>
/// <remarks>
/// A handler rather than a header captured at startup: the token expires, and a header set once would be a
/// working agent that stops working an hour later with nothing to say why.
/// </remarks>
internal sealed class BearerHandler(ICatalogAccessToken token) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await token.GetAsync(cancellationToken).ConfigureAwait(false));

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
