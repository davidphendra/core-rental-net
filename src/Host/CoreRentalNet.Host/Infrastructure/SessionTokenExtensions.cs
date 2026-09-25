using Microsoft.AspNetCore.Authentication;

namespace CoreRentalNet.Host.Infrastructure;

/// <summary>Reads the signed-in account's token from the sign-in session.</summary>
/// <remarks>
/// <para>
/// The OIDC handler saved the access token at sign-in (<c>SaveTokens</c>) and the cookie holds the ticket, so
/// the scheme is left to the default. This is the same read <see cref="TokenHandler"/> performs for an outbound
/// call; it exists so a run can read the token without doing so as a side effect of an HTTP client.
/// </para>
/// <para>
/// It sits beside <see cref="TokenHandler"/> because it is the same concern — the account's access token — read
/// once for a value rather than attached to a request.
/// </para>
/// </remarks>
internal static class SessionTokenExtensions
{
    /// <summary>The name the OIDC handler saves the access token under.</summary>
    private const string Name = "access_token";

    /// <summary>The token, or null when this request has no session, or the session holds none.</summary>
    public static async Task<string?> GetAccessTokenAsync(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return await context.GetTokenAsync(Name).ConfigureAwait(false);
    }
}
