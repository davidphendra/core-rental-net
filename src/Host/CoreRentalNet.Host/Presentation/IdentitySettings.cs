namespace CoreRentalNet.Host.Presentation;

/// <summary>
/// Whether this deployment has an identity provider at all.
/// </summary>
/// <remarks>
/// Authentication is optional and additive. With no domain configured the application
/// registers no OIDC scheme, hides the sign-in affordance and behaves exactly as it did before
/// identity existed — a working application must not refuse to start because of a feature nobody
/// is using. The client secret is never read from a committed file; it comes from user secrets
/// locally and from the environment otherwise.
/// </remarks>
public sealed record IdentitySettings
{
    /// <summary>The scopes a deployment that names none gets.</summary>
    public const string DefaultScope = "openid profile email";

    private IdentitySettings(bool enabled, string? domain, string? clientId, string? clientSecret, string? authority, string roleClaimType, string? audience, string scope)
    {
        Enabled = enabled;
        Domain = domain;
        ClientId = clientId;
        ClientSecret = clientSecret;
        Authority = authority;
        RoleClaimType = roleClaimType;
        Audience = audience;
        Scope = scope;
    }

    /// <summary>
    /// Whether this deployment signs anyone in at all, whatever credentials it happens to hold.
    /// </summary>
    /// <remarks>
    /// On unless configuration says otherwise, because the safe direction for a switch like this is the
    /// one that keeps the gate closed. It exists for two reasons: so that running the demonstration
    /// without identity is something a deployment says out loud rather than something that happens by
    /// leaving credentials out, and so that the browser suite can run the application the way it ships
    /// even on a machine whose local settings file holds real credentials - which is the difference
    /// between a hermetic suite and one that changes behaviour with the developer's laptop.
    /// </remarks>
    public bool Enabled { get; }

    public string? Domain { get; }

    public string? ClientId { get; }

    public string? ClientSecret { get; }

    /// <summary>
    /// The provider's issuer address, when it is not the one its <see cref="Domain"/> implies.
    /// </summary>
    /// <remarks>
    /// Auth0's wrapper derives the authority from the domain as <c>https://{domain}</c>, which is
    /// right for a tenant and impossible for a provider running on localhost over plain HTTP. This
    /// overrides it, so the browser suite can drive a real OIDC handshake against a provider in this
    /// repository through the same setting a real deployment would use. Absent, the
    /// domain decides, exactly as before.
    /// </remarks>
    public string? Authority { get; }

    /// <summary>
    /// Where the provider puts the account's roles, which is its Action's namespace to choose.
    /// </summary>
    /// <remarks>
    /// Auth0 issues roles in no token by default, so this names a claim the tenant's post-login
    /// Action must be written to emit. It is configuration rather than a constant because the
    /// namespace belongs to the Action, not to this application; the default is the one this
    /// project's own permissions claim already uses, so one Action can emit both (see the README).
    /// </remarks>
    public string RoleClaimType { get; }

    /// <summary>
    /// The API whose access token carries the account's permissions, when one is asked for.
    /// </summary>
    /// <remarks>
    /// Auth0 never puts permissions on an ID token, and a post-login Action cannot see them either,
    /// so the only place they can be read is an access token - and Auth0 issues one only when the
    /// login names an API's identifier as the audience (see the README). Optional: without it the
    /// application signs in exactly as before and no permission claim is produced.
    /// </remarks>
    public string? Audience { get; }

    /// <summary>
    /// The scopes the login asks the provider for, in one space-separated string.
    /// </summary>
    /// <remarks>
    /// The provider's own default omits the address this application records on an order, so the
    /// default here always asks for <c>profile email</c>. A deployment whose API is set to per-app
    /// authorization must also name each permission it wants as a scope - Auth0 issues a permission
    /// only when the application actually requested it - so that name is configuration, beside the
    /// audience it belongs to, rather than a string in code.
    /// </remarks>
    public string Scope { get; }

    /// <summary>True only when there is enough configuration to authenticate at all.</summary>
    public bool IsConfigured =>
        Enabled && !string.IsNullOrWhiteSpace(Domain) && !string.IsNullOrWhiteSpace(ClientId);

    public static IdentitySettings From(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new IdentitySettings(
            !string.Equals(configuration["Auth0:Enabled"]?.Trim(), "false", StringComparison.OrdinalIgnoreCase),
            configuration["Auth0:Domain"]?.Trim(),
            configuration["Auth0:ClientId"]?.Trim(),
            configuration["Auth0:ClientSecret"]?.Trim(),
            configuration["Auth0:Authority"]?.Trim().TrimEnd('/') is { Length: > 0 } authority
                ? authority
                : null,
            configuration["Auth0:RoleClaimType"]?.Trim() is { Length: > 0 } claimType
                ? claimType
                : CurrentCustomer.DefaultRoleClaimType,
            configuration["Auth0:Audience"]?.Trim() is { Length: > 0 } audience
                ? audience
                : null,
            configuration["Auth0:Scope"]?.Trim() is { Length: > 0 } scope
                ? scope
                : DefaultScope);
    }
}
