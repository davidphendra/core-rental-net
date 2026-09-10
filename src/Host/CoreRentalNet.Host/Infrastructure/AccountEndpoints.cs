using System.Security.Claims;
using CoreRentalNet.Host.Presentation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace CoreRentalNet.Host.Infrastructure;

/// <summary>
/// The only HTTP surface this application has, and the only place it can be: a circuit has no
/// <c>HttpContext</c>, so it cannot challenge, set a cookie or redirect (ADR-0017).
/// </summary>
public static class AccountEndpoints
{
    public const string CallbackPath = "/account/callback";

    public static void Map(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/account/login", (string? returnUrl, HttpContext context) =>
        {
            var destination = LocalUrl.Sanitise(returnUrl);

            return Results.Challenge(
                new AuthenticationProperties { RedirectUri = destination },
                [OpenIdConnectDefaults.AuthenticationScheme]);
        });

        app.MapGet("/account/logout", (HttpContext context) =>
        {
            // Signing out of the cookie alone would leave the provider's own session alive, so on a
            // shared computer the next click would silently sign the same person back in.
            return Results.SignOut(
                new AuthenticationProperties { RedirectUri = "/" },
                [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]);
        });
    }
}

/// <summary>
/// Keeps the login page from becoming an open redirect.
/// </summary>
/// <remarks>
/// This is the framework's own rule: a local URL starts with a single forward slash, and not with
/// two, and not with a backslash that a browser might treat as one.
/// </remarks>
internal static class LocalUrl
{
    public static string Sanitise(string? candidate, string fallback = "/")
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return fallback;
        }

        var url = candidate.Trim();

        return IsLocal(url) ? url : fallback;
    }

    public static bool IsLocal(string? url)
        => !string.IsNullOrEmpty(url)
           && url[0] == '/'
           && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'));
}
