using Microsoft.Extensions.Configuration;

namespace CoreRentalNet.Host.Presentation;

/// <summary>
/// The claim a permission is checked against, as the identity provider issues it.
/// </summary>
/// <remarks>
/// Both halves come from configuration, because they are not this application's to invent: the claim is
/// added by the provider's own rule, and its name and value are whatever that rule was written to
/// produce. Blank means unconfigured, which the requirement decides the meaning of - a permission that
/// opens when it cannot say what it checks would otherwise be a licence rather than a rule.
/// </remarks>
public sealed record ClaimSettings
{
    /// <summary>The configuration section for reading the catalogue.</summary>
    public const string CatalogRead = "CatalogRead";

    /// <summary>The configuration section for the AI workspace builder, which is closed when it is unset.</summary>
    public const string AiUse = "AIUse";

    private ClaimSettings(string? claimType, string? claimValue)
    {
        ClaimType = claimType;
        ClaimValue = claimValue;
    }

    public string? ClaimType { get; }

    public string? ClaimValue { get; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClaimType) && !string.IsNullOrWhiteSpace(ClaimValue);

    /// <summary>One permission's settings, read from the section named after it.</summary>
    public static ClaimSettings From(IConfiguration configuration, string section)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new ClaimSettings(
            configuration[$"Authorization:{section}:ClaimType"]?.Trim(),
            configuration[$"Authorization:{section}:ClaimValue"]?.Trim());
    }
}
