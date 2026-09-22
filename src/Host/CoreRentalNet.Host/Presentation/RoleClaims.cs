using System.Security.Claims;

namespace CoreRentalNet.Host.Presentation;

/// <summary>
/// The roles an access token's permissions name, lifted into the identity.
/// </summary>
/// <remarks>
/// <para>
/// Auth0 issues a role in no token on its own: a role is a bundle of permissions, and it is the
/// permissions that the access token carries. A tenant that names one permission per role states
/// the role inside the permission - this project's tenant uses <c>manager:role</c>,
/// <c>supervisor:role</c>, <c>staff:role</c> and <c>guest:role</c> - so the role can be read back
/// from it, which is one fewer thing for an Action to write into the ID token.
/// </para>
/// <para>
/// The suffix is the tenant's convention, not an Auth0 rule, so a permission that does not carry it
/// is left alone: the one that entitles a reader to the catalogService names no role. The name before the
/// suffix is shown as the tenant spells it, with its first letter capitalised.
/// </para>
/// </remarks>
internal static class RoleClaims
{
    /// <summary>The suffix a permission carries when it stands for the role of the same name.</summary>
    public const string PermissionSuffix = ":role";

    /// <summary>Adds the roles the access token's permissions name, under <paramref name="claimType"/>.</summary>
    public static void AddTo(ClaimsPrincipal? principal, string? accessToken, string? claimType)
    {
        if (principal?.Identity is not ClaimsIdentity identity || string.IsNullOrWhiteSpace(claimType))
        {
            return;
        }

        foreach (var permission in PermissionClaims.Read(accessToken))
        {
            if (permission.Length <= PermissionSuffix.Length
                || !permission.EndsWith(PermissionSuffix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var role = Label(permission[..^PermissionSuffix.Length]);

            // An Action may already have written the role into the ID token under this same claim;
            // the role is the same answer, so it is stated once.
            if (!identity.HasClaim(claimType, role))
            {
                identity.AddClaim(new Claim(claimType, role));
            }
        }
    }

    /// <summary>The role's name, with the first letter as the tenant's capitalised values spell it.</summary>
    private static string Label(string name)
        => name.Length == 0 ? name : string.Concat(char.ToUpperInvariant(name[0]), name[1..]);
}
