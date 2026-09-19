using CoreRentalNet.Host.Presentation;
using Microsoft.AspNetCore.Authorization;

namespace CoreRentalNet.Host.Infrastructure;

/// <summary>
/// A permission: the claim that grants it, and what it does when nobody can be checked.
/// </summary>
/// <remarks>
/// It carries its claim rather than being answered from ambient configuration, because the claim is the
/// provider's to name and not this application's to assume. It carries its unconfigured behaviour for the
/// same reason: the answer belongs to each permission, not to the handler that answers them all.
/// </remarks>
internal sealed class ClaimRequirement(ClaimSettings settings, UnconfiguredBehaviour whenUnconfigured)
    : IAuthorizationRequirement
{
    public ClaimSettings Settings { get; } = settings;

    /// <summary>What this permission does when the deployment has no identity provider at all.</summary>
    public UnconfiguredBehaviour WhenUnconfigured { get; } = whenUnconfigured;
}
