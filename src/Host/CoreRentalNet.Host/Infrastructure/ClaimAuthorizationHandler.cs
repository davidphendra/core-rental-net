using CoreRentalNet.Host.Presentation;
using Microsoft.AspNetCore.Authorization;

namespace CoreRentalNet.Host.Infrastructure;

/// <summary>
/// Answers every <see cref="ClaimRequirement"/>: one rule, so entitlements cannot drift apart.
/// </summary>
/// <remarks>
/// <para>
/// An application whose identity is optional has to say what it does when there is none, and the answer
/// is the requirement's to state rather than this handler's to assume: a requirement that says
/// <see cref="ClaimBehavior.Open"/> is satisfied, and one that says <see cref="ClaimBehavior.Closed"/>
/// is not. The alternative readings are both worse - refusing everything makes a demonstration that
/// boots without configuration unusable, and inventing a development identity would be a fake, which
/// the browser suite forbids.
/// </para>
/// <para>
/// When a provider <em>is</em> configured the answer is closed unless both halves hold: the account is
/// authenticated, and it carries the configured claim with the configured value. A blank claim
/// configuration therefore denies rather than allows - a deployment that has a provider and cannot say
/// which claim entitles a caller has not been finished, and the safe reading of an unfinished rule is
/// nobody. The comparison is exact and case-sensitive: matching a configured value as a substring would
/// let a longer value through.
/// </para>
/// </remarks>
internal sealed class ClaimAuthorizationHandler(IdentitySettings identity)
    : AuthorizationHandler<ClaimRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ClaimRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        if (!identity.IsConfigured)
        {
            if (requirement.WhenUnconfigured == ClaimBehavior.Open)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }

        var claim = requirement.Settings;

        if (context.User.Identity?.IsAuthenticated == true
            && claim.IsConfigured
            && context.User.HasClaim(claim.ClaimType!, claim.ClaimValue!))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
