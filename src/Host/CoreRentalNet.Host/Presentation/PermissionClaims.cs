using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CoreRentalNet.Host.Presentation;

/// <summary>
/// The permissions an access token carries, lifted into the identity.
/// </summary>
/// <remarks>
/// <para>
/// Auth0 puts an account's permissions on the <em>access</em> token, never on the ID token, and a
/// post-login Action cannot see them — its <c>event.authorization</c> carries roles only, an
/// acknowledged gap. The one place they can be read is therefore the access token that arrives in
/// the same code exchange as the ID token this application has already validated. The permissions
/// become claims and the token itself is still not stored.
/// </para>
/// <para>
/// The signature is not verified again. The token arrived over TLS from the provider's own token
/// endpoint, in the response whose ID token the handler validated moments earlier, so it is the
/// provider's; re-verifying it would need the API's audience and signing keys for no gain. If it
/// cannot be read at all, nothing is added — reading it is a convenience, not the authorization
/// decision, and every gate still fails closed.
/// </para>
/// </remarks>
internal static class PermissionClaims
{
    /// <summary>The standard claim Auth0 writes when RBAC is enabled and permissions are included.</summary>
    public const string AccessTokenClaimType = "permissions";

    /// <summary>Adds the access token's permissions to the principal under <paramref name="claimType"/>.</summary>
    public static void AddTo(ClaimsPrincipal? principal, string? accessToken, string? claimType)
    {
        if (principal?.Identity is not ClaimsIdentity identity || string.IsNullOrWhiteSpace(claimType))
        {
            return;
        }

        foreach (var permission in Read(accessToken))
        {
            identity.AddClaim(new Claim(claimType, permission));
        }
    }

    /// <summary>The permissions the access token carries, or empty when it carries none.</summary>
    public static IReadOnlyList<string> Read(string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return [];
        }

        try
        {
            return new JsonWebToken(accessToken)
                .Claims
                .Where(claim => string.Equals(claim.Type, AccessTokenClaimType, StringComparison.Ordinal))
                .Select(claim => claim.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }
        catch (Exception exception) when (exception is SecurityTokenException or ArgumentException)
        {
            return [];
        }
    }
}
