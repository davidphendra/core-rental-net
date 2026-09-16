using Auth0.AspNetCore.Authentication;
using CoreRentalNet.Host.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace CoreRentalNet.Host.Controllers;

/// <summary>
/// The only HTTP surface this application has, and the only place it can be: a circuit has no
/// <c>HttpContext</c>, so it cannot challenge, set a cookie or redirect.
/// </summary>
/// <remarks>
/// Written as endpoint mappings rather than an MVC controller on purpose. It is the whole of the
/// application's controller layer, the two routes are redirects with no input to bind and no body
/// to render, and an <c>MvcController</c> would bring the model binding and filter pipeline into an
/// application that otherwise has none. Converting it is a small change if the project
/// ever grows real endpoints.
/// </remarks>
public static class AccountController
{
    /// <summary>Where the identity provider returns the browser after signing in.</summary>
    /// <remarks>
    /// The only address of ours the tenant has to be told about. Sign-out does not come back to a
    /// route here: the provider's own logout endpoint is handed the application root to return to,
    /// so that is what belongs in the tenant's "Allowed Logout URLs" list.
    /// </remarks>
    public const string CallbackPath = "/account/callback";

    /// <summary>The address that starts a sign-in, and the one that ends a session.</summary>
    public const string SignInPath = "/account/login";

    public const string SignOutPath = "/account/logout";

    /// <summary>
    /// Maps the two routes a customer can reach under <c>/account</c>. The callback path above is
    /// deliberately absent: the authentication handler owns it and the middleware answers it before
    /// routing is reached, so an endpoint here would be dead code.
    /// </summary>
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var account = endpoints.MapGroup("/account");

        account.MapGet("login", (string? returnUrl) =>
        {
            var destination = LocalUrl.Sanitise(returnUrl);

            return Results.Challenge(
                new AuthenticationProperties { RedirectUri = destination },
                [Auth0Constants.AuthenticationScheme]);
        });

        account.MapGet("logout", () =>
        {
            // Signing out of the cookie alone would leave the provider's own session alive, so on a
            // shared computer the next click would silently sign the same person back in. The
            // provider's scheme ends that session and sends the browser back to the root.
            return Results.SignOut(
                new AuthenticationProperties { RedirectUri = "/" },
                [Auth0Constants.AuthenticationScheme, CookieAuthenticationDefaults.AuthenticationScheme]);
        });
    }
}
