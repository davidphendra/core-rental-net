namespace CoreRentalNet.Host.Presentation;

/// <summary>
/// Whether this deployment has an identity provider at all.
/// </summary>
/// <remarks>
/// Authentication is optional and additive (ADR-0016). With no domain configured the application
/// registers no OIDC scheme, hides the sign-in affordance and behaves exactly as it did before
/// identity existed — a working application must not refuse to start because of a feature nobody
/// is using. The client secret is never read from a committed file; it comes from user secrets
/// locally and from the environment otherwise.
/// </remarks>
public sealed record IdentitySettings
{
    private IdentitySettings(bool enabled, string? domain, string? clientId, string? clientSecret)
    {
        Enabled = enabled;
        Domain = domain;
        ClientId = clientId;
        ClientSecret = clientSecret;
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
            configuration["Auth0:ClientSecret"]?.Trim());
    }
}
