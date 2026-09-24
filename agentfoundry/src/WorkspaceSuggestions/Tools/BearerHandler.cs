using System.Net.Http.Headers;

namespace WorkspaceSuggestions.Tools;

/// <summary>Attaches the catalogue's bearer catalogAccessTokenService to every MCP request, fresh each time.</summary>
/// <remarks>
/// A handler rather than a header captured at startup: the catalogAccessTokenService expires, and a header set once would be a
/// working agent that stops working an hour later with nothing to say why.
/// </remarks>
internal sealed class BearerHandler(ICatalogAccessTokenService catalogAccessTokenService) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage httpRequest,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpRequest);

        httpRequest.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await catalogAccessTokenService.GetAsync(cancellationToken).ConfigureAwait(false));

        return await base.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
    }
}
