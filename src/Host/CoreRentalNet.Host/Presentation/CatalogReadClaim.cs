namespace CoreRentalNet.Host.Presentation;

/// <summary>
/// The claim an account must carry to read the catalog, as the identity provider issues it.
/// </summary>
/// <remarks>
/// Both halves come from configuration, because they are not this application's to invent: the claim
/// is added by the provider's own rule, and its name and value are whatever that rule was written to
/// produce. Blank means unconfigured, which the policy treats as nobody being entitled rather than
/// everybody - a deployment that says it checks a claim and does not know which one has a
/// misconfiguration, not a licence.
/// </remarks>
public sealed record CatalogReadClaim
{
    private CatalogReadClaim(string? claimType, string? claimValue)
    {
        ClaimType = claimType;
        ClaimValue = claimValue;
    }

    public string? ClaimType { get; }

    public string? ClaimValue { get; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClaimType) && !string.IsNullOrWhiteSpace(ClaimValue);

    public static CatalogReadClaim From(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new CatalogReadClaim(
            configuration["Authorization:CatalogRead:ClaimType"]?.Trim(),
            configuration["Authorization:CatalogRead:ClaimValue"]?.Trim());
    }
}
