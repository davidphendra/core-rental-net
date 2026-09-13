using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;

namespace CoreRentalNet.Host.Infrastructure;

/// <summary>
/// Puts the signed-in account's access token on an outbound request to the API the audience names.
/// </summary>
/// <remarks>
/// <para>
/// In a Blazor Server application the circuit is a SignalR connection rather than an HTTP request,
/// so a component cannot read the token for itself. A delegating handler runs with the request that
/// carries it, which is the one place it can be attached without the application keeping a second
/// copy of the token.
/// </para>
/// <para>
/// Outside a request, or before sign-in, there is no token to attach; the request is sent without
/// one rather than with an empty header, so the API answers with its own 401 rather than being
/// handed a header it has to interpret.
/// </para>
/// </remarks>
public sealed class TokenHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    /// <summary>The named client this handler is attached to, so the registration and a caller agree.</summary>
    public const string ClientName = "Auth0Api";

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (httpContextAccessor.HttpContext is { } context)
        {
            var accessToken = await context.GetTokenAsync("access_token").ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
