using Auth0.AspNetCore.Authentication;
using CoreRentalNet.Host.Infrastructure;
using CoreRentalNet.Host.Presentation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace CoreRentalNet.Host.Controllers;

/// <summary>The two browser routes under <c>/account</c>: the one that starts a sign-in, and the one that ends a session.</summary>
/// <remarks>
/// <para>
/// A controller, and this file used to be the one place that was not. Both routes are redirects, which is what an
/// action result is for, so the shape they need is a method rather than a mapping. Each constant is its own route
/// template, so the address the sign-out affordances are written from and the address that answers cannot drift -
/// which the mapping they replaced could, because it repeated the segments and named the constants separately.
/// </para>
/// <para>
/// <b><c>[ApiController]</c>, like every other controller here, and the attribute is also load-bearing.</b>
/// <see cref="InternalControllerFeatureProvider"/> admits an internal type only when the type itself carries
/// <c>[Controller]</c>, which this derives from. Without it, MVC would not discover this controller and the two
/// routes would not exist at all.
/// </para>
/// <para>
/// <b>Nothing is answered where there is no provider to sign in at.</b> Such a deployment registers no <c>Auth0</c>
/// scheme, so a route that challenged anyway would report a missing feature as a broken one. The provider is a
/// dependency rather than an assumption for that reason: the routes stay published and refuse. Because
/// <c>[ApiController]</c> maps a client error, that refusal arrives as problem details rather than as an empty
/// status - the same 404 routing used to answer with by not publishing them, in the one shape this controller can
/// produce.
/// </para>
/// </remarks>
[ApiController]
internal sealed class AccountController(IdentitySettings identity) : ControllerBase
{
    /// <summary>Where the identity provider returns the browser after signing in.</summary>
    /// <remarks>
    /// The only address of ours the tenant has to be told about. Sign-out does not come back to a route here: the
    /// provider's own logout endpoint is handed the application root to return to, so that is what belongs in the
    /// tenant's "Allowed Logout URLs" list.
    /// </remarks>
    public const string CallbackPath = "/account/callback";

    /// <summary>The address that starts a sign-in, and the one that ends a session.</summary>
    public const string SignInPath = "/account/login";

    public const string SignOutPath = "/account/logout";

    /// <summary>Starts a sign-in at the provider, and returns the browser to where it was going.</summary>
    [HttpGet(SignInPath)]
    public IActionResult Login([FromQuery] string? returnUrl)
        => ThereIsAProviderToSignInAt
            ? Challenge(
                new AuthenticationProperties { RedirectUri = LocalUrl.Sanitise(returnUrl) },
                Auth0Constants.AuthenticationScheme)
            : NotFound();

    /// <summary>Ends the session here and at the provider.</summary>
    /// <remarks>
    /// Signing out of the cookie alone would leave the provider's own session alive, so on a shared computer the
    /// next click would silently sign the same person back in. The provider's own endpoint ends that session and
    /// sends the browser back to the root, so both schemes are named.
    /// </remarks>
    [HttpGet(SignOutPath)]
    public IActionResult Logout()
        => ThereIsAProviderToSignInAt
            ? SignOut(
                new AuthenticationProperties { RedirectUri = "/" },
                Auth0Constants.AuthenticationScheme,
                CookieAuthenticationDefaults.AuthenticationScheme)
            : NotFound();

    /// <summary>Whether this deployment has a provider at all, and so a scheme to challenge.</summary>
    private bool ThereIsAProviderToSignInAt => identity.IsConfigured;
}
