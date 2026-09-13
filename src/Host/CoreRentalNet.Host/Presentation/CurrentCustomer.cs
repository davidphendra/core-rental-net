using System.Security.Claims;
using System.Text.Json.Serialization;

namespace CoreRentalNet.Host.Presentation;

/// <summary>
/// Who is signed in, as the interactive tree needs to know.
/// </summary>
/// <remarks>
/// Passed into the circuit as a component parameter from the root component, exactly like the
/// draft token, because a value declared by a statically-rendered parent does not cross into an
/// interactive render boundary.
/// <para>
/// A parameter that crosses that boundary is round-tripped through JSON, so this type has to be
/// one the serializer can rebuild: a public constructor whose parameters match its properties, and
/// nothing computed being mistaken for state. An earlier version had a private constructor and the
/// circuit silently refused to start — the page prerendered perfectly and then never became
/// interactive.
/// </para>
/// <para>
/// The claims are the provider's, and Auth0 only issues what the requested scopes entitle it to
/// (the <c>openid profile email</c> scopes this application asks for). Each is read under every
/// name it may arrive under, standard or short OIDC, because the provider may or may not map
/// inbound claims, and a value silently missing reads as an account without one.
/// </para>
/// </remarks>
public sealed record CurrentCustomer(string? Subject, string? Email, string? Name, string? Role)
{
    /// <summary>
    /// Where a post-login Action puts the account's roles, unless configuration names another.
    /// </summary>
    /// <remarks>
    /// Auth0 does not put roles in a token on its own. The documented way is an Action that calls
    /// <c>api.idToken.setCustomClaim(namespace + "/roles", event.authorization.roles)</c>, and the
    /// namespace is the provider's rule's to choose, so the claim is configuration here rather
    /// than a string this application writes down. The default matches the namespace this project's
    /// permissions claim already uses, so one Action can emit both.
    /// </remarks>
    public const string DefaultRoleClaimType = "https://core-rental.periang.auth0/roles";

    public static CurrentCustomer Anonymous { get; } = new(null, null, null, null);

    [JsonIgnore]
    public bool IsSignedIn => !string.IsNullOrWhiteSpace(Subject);

    /// <summary>What is drawn instead of the provider's picture, so no page reaches a third party.</summary>
    [JsonIgnore]
    public string DisplayName => Name ?? Email ?? "Account";

    /// <summary>
    /// The account's roles, in the order the provider issued them, or null when it issued none.
    /// </summary>
    /// <remarks>
    /// Nothing is assumed when the claim is absent. A deployment whose Action does not emit the
    /// claim has no role to show, and the page says exactly that; printing a product-owned label
    /// where the provider's answer belongs would be the application inventing an entitlement.
    /// </remarks>
    [JsonIgnore]
    public string? RoleLabel => Role;

    [JsonIgnore]
    public string Initials
    {
        get
        {
            var source = Name ?? Email ?? string.Empty;
            var words = source.Split([' ', '.', '@', '_', '-'], StringSplitOptions.RemoveEmptyEntries);

            return words.Length switch
            {
                0 => "?",
                1 => words[0][..1].ToUpperInvariant(),
                _ => string.Concat(words[0][..1], words[1][..1]).ToUpperInvariant(),
            };
        }
    }

    public static CurrentCustomer FromClaims(ClaimsPrincipal? principal, string? roleClaimType = null)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return Anonymous;
        }

        return new CurrentCustomer(
            Read(principal, ClaimTypes.NameIdentifier, "sub"),
            Read(principal, ClaimTypes.Email, "email"),
            ReadName(principal),
            ReadRole(principal, roleClaimType ?? DefaultRoleClaimType));
    }

    /// <summary>
    /// The account's name, as the OIDC <c>profile</c> scope describes it.
    /// </summary>
    /// <remarks>
    /// The provider's <c>name</c> is used when it is a name, but it is not trusted to be one: for an
    /// account that signed up with an email and nothing else, Auth0 fills <c>name</c> with the
    /// address, and a page that prints the email where a name belongs looks broken. A value that is
    /// an address is therefore passed over in favour of the name parts (<c>given_name</c> and
    /// <c>family_name</c>), and then of a nickname that is not one. When all the provider has is the
    /// address, the answer is null and the page states that rather than repeating the email.
    /// </remarks>
    private static string? ReadName(ClaimsPrincipal principal)
    {
        var name = RealName(Read(principal, ClaimTypes.Name, "name", "unique_name"));

        if (name is not null)
        {
            return name;
        }

        var parts = new[]
            {
                Read(principal, ClaimTypes.GivenName, "given_name"),
                Read(principal, ClaimTypes.Surname, "family_name"),
            }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        return parts.Length > 0
            ? string.Join(' ', parts)
            : RealName(Read(principal, "nickname"));
    }

    /// <summary>A value that can stand as a name: present, and not an email address.</summary>
    private static string? RealName(string? value)
        => string.IsNullOrWhiteSpace(value) || value.Contains('@', StringComparison.Ordinal) ? null : value;

    /// <summary>
    /// Every role the account carries, in the order the provider issued them, or null when it issued
    /// none.
    /// </summary>
    /// <remarks>
    /// The configured claim is read first, because that is the one Auth0's Action writes. A claim
    /// whose own name ends in <c>/roles</c> or <c>/role</c> is read too: the namespace belongs to the
    /// tenant's Action, and an Action written against a different one is still an Action that stated
    /// the account's roles. The standard role claims are read for a provider that is not Auth0, and
    /// the short OIDC forms because a provider that maps claims may deliver them either way. Auth0
    /// emits an array, and a JSON-array claim arrives as one claim per element, so every role is
    /// collected rather than the first.
    /// </remarks>
    private static string? ReadRole(ClaimsPrincipal principal, string? roleClaimType)
    {
        var configured = string.IsNullOrWhiteSpace(roleClaimType) ? DefaultRoleClaimType : roleClaimType;

        var roles = principal.Claims
            .Where(claim => IsRoleClaim(claim.Type, configured))
            // The configured claim's roles come first; OrderBy is stable, so within each group the
            // provider's own order survives.
            .OrderByDescending(claim => string.Equals(claim.Type, configured, StringComparison.Ordinal))
            .Select(claim => claim.Value.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return roles.Length == 0 ? null : string.Join(", ", roles);
    }

    private static bool IsRoleClaim(string claimType, string configured)
        => string.Equals(claimType, configured, StringComparison.Ordinal)
           || string.Equals(claimType, ClaimTypes.Role, StringComparison.Ordinal)
           || string.Equals(claimType, "role", StringComparison.Ordinal)
           || string.Equals(claimType, "roles", StringComparison.Ordinal)
           || claimType.EndsWith("/roles", StringComparison.Ordinal)
           || claimType.EndsWith("/role", StringComparison.Ordinal);

    private static string? Read(ClaimsPrincipal principal, params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = principal.FindFirst(claimType)?.Value;

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
