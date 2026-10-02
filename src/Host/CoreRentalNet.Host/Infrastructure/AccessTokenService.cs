using Auth0.AspNetCore.Authentication;

namespace CoreRentalNet.Host.Infrastructure;

/// <summary>Reads the caller's token for a run through the identity SDK, and refuses when it cannot.</summary>
/// <remarks>
/// <para>
/// <b>The identity SDK is the whole of the expiry handling.</b> Its own read returns the login-time token while
/// the recorded expiry is still ahead of now by the SDK's own margin, exchanges the session's refresh token
/// when it is not, and reports null when it can produce neither. That answer, and nothing this application
/// computes, is what becomes a token or a failure.
/// </para>
/// <para>
/// <b>A null answer is a failure rather than a value.</b> A run started with no token would be paid for and
/// then fail at the catalogue's far end, so the caller is told before the run begins instead.
/// </para>
/// <para>
/// <b>Call this before anything is written to the response.</b> A refresh that succeeds persists the rotated
/// token by writing the authentication cookie, which cannot happen once the response has started.
/// </para>
/// </remarks>
internal sealed class AccessTokenService(IHttpContextAccessor httpContextAccessor) : IAccessTokenService
{
    /// <inheritdoc />
    public async Task<string> GetAsync()
    {
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException(
                "The caller's access token is read from the request, and there is no request in flight.");

        var accessToken = await httpContext.GetAccessTokenAsync(new AccessTokenRequest())
                                    .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new CallerAccessTokenUnavailableException(
                "The session could not produce an access token for the catalogue.");
        }

        return accessToken;
    }
}
