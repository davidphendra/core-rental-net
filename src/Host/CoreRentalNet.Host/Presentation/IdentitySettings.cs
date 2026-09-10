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
    private IdentitySettings(string? domain, string? clientId, string? clientSecret)
    {
        Domain = domain;
        ClientId = clientId;
        ClientSecret = clientSecret;
    }

    public string? Domain { get; }

    public string? ClientId { get; }

    public string? ClientSecret { get; }

    /// <summary>True only when there is enough configuration to authenticate at all.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Domain) && !string.IsNullOrWhiteSpace(ClientId);

    public static IdentitySettings From(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new IdentitySettings(
            configuration["Auth0:Domain"]?.Trim(),
            configuration["Auth0:ClientId"]?.Trim(),
            configuration["Auth0:ClientSecret"]?.Trim());
    }
}
