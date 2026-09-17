using CoreRentalNet.Host.Presentation;
using Microsoft.AspNetCore.Authorization;

namespace CoreRentalNet.Host.Infrastructure;

/// <summary>
/// A permission: the claim that grants it, and what it means where there is no provider.
/// </summary>
/// <remarks>
/// It carries its claim rather than being answered from ambient configuration, because there is more
/// than one permission now and their unconfigured rules differ. A requirement that carried nothing
/// could be answered the same way for all of them, which is right for exactly one of them.
/// </remarks>
internal sealed class ClaimRequirement(ClaimSettings settings, ClaimBehavior whenUnconfigured)
    : IAuthorizationRequirement
{
    public ClaimSettings Settings { get; } = settings;

    public ClaimBehavior WhenUnconfigured { get; } = whenUnconfigured;
}
