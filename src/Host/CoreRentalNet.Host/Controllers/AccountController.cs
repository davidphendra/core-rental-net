using CoreRentalNet.Host.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace CoreRentalNet.Host.Controllers;

/// <summary>
/// The only HTTP surface this application has, and the only place it can be: a circuit has no
/// <c>HttpContext</c>, so it cannot challenge, set a cookie or redirect (ADR-0017).
/// </summary>
/// <remarks>
/// Written as endpoint mappings rather than an MVC controller on purpose. It is the whole of the
/// application's controller layer, the two routes are redirects with no input to bind and no body
/// to render, and an <c>MvcController</c> would bring the model binding and filter pipeline into an
/// application that otherwise has none (ADR-0012). Converting it is a small change if the project
/// ever grows real endpoints.
/// </remarks>
public static class AccountController
{
    /// <summary>Where the identity provider returns the browser after signing in.</summary>
    public const string CallbackPath = "/account/callback";

    /// <summary>
    /// Where the provider sends the browser after signing out, which the handler then redirects
    /// from. This is the second URL the tenant has to be told about.
    /// </summary>
    public const string SignedOutCallbackPath = "/account/signed-out";

    /// <summary>
    /// Maps the two routes a customer can reach under <c>/account</c>. The callback paths above are
    /// deliberately absent: the authentication handler owns those and the middleware answers them
    /// before routing is reached, so an endpoint here would be dead code.
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
                [OpenIdConnectDefaults.AuthenticationScheme]);
        });

        account.MapGet("logout", () =>
        {
            // Signing out of the cookie alone would leave the provider's own session alive, so on a
            // shared computer the next click would silently sign the same person back in.
            return Results.SignOut(
                new AuthenticationProperties { RedirectUri = "/" },
                [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]);
        });
    }
}
