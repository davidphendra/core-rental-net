using CoreRentalNet.Host.Presentation;
using Microsoft.AspNetCore.Authorization;

namespace CoreRentalNet.Host.Infrastructure;

/// <summary>
/// Answers <see cref="CatalogReadRequirement"/>.
/// </summary>
/// <remarks>
/// <para>
/// An application whose identity is optional has to say what it does when there is none. Here that is
/// the first branch: with no provider configured, this deployment cannot authenticate anybody, so
/// there is no reader to check and the catalog is open. The alternative readings are worse - refusing
/// everyone would make a demonstration that boots without configuration unusable, and inventing a
/// development identity would be a fake, which the browser suite forbids.
/// </para>
/// <para>
/// When a provider <em>is</em> configured the answer is closed unless both halves hold: the account is
/// authenticated, and it carries the configured claim with the configured value. A blank claim
/// configuration therefore denies rather than allows - a deployment that has a provider and cannot say
/// which claim entitles a reader has not been finished, and the safe reading of an unfinished rule is
/// nobody.
/// </para>
/// <para>
/// The comparison is exact and case-sensitive. A claim value is not a search: matching the configured
/// value as a substring would let a longer value through, and that is the kind of check that looks
/// right until somebody composes one that contains it.
/// </para>
/// </remarks>
internal sealed class CatalogReadAuthorizationHandler(
    IdentitySettings identity,
    CatalogReadClaim claim) : AuthorizationHandler<CatalogReadRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CatalogReadRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!identity.IsConfigured)
        {
            context.Succeed(requirement);

            return Task.CompletedTask;
        }

        if (context.User.Identity?.IsAuthenticated == true
            && claim.IsConfigured
            && context.User.HasClaim(claim.ClaimType!, claim.ClaimValue!))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
