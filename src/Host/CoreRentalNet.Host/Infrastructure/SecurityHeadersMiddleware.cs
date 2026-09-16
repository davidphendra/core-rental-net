using CoreRentalNet.Host.Presentation;
using Microsoft.AspNetCore.Hosting;

namespace CoreRentalNet.Host.Infrastructure;

/// <summary>
/// The security headers every response carries, in one place.
/// </summary>
/// <remarks>
/// <para>
/// The policy is <c>'self'</c> and nothing else, with exactly two exceptions and both written down:
/// <c>data:</c> images, which is how the compiled stylesheet draws the native form controls, and
/// <c>'none'</c> for framing, because an application that is never framed refuses to be framed at all.
/// The application references no external script, stylesheet, font, image or endpoint, so a strict
/// policy costs it nothing - which is why it is written strictly rather than permissively.
/// </para>
/// <para>
/// <c>connect-src 'self'</c> is deliberate rather than copied. The Blazor circuit is a same-origin
/// WebSocket, and the framework's own guidance offers <c>ws:</c> and <c>wss:</c> as well - but those
/// schemes permit a socket to <em>any</em> host, which is the opposite of what this header is for. If
/// a browser ever refuses the same-origin socket under <c>'self'</c>, the answer is to name the origin,
/// not the scheme.
/// </para>
/// <para>
/// It is enforced. It was delivered as <c>Content-Security-Policy-Report-Only</c> first, because the
/// failure mode of a wrong policy is a page that renders perfectly and never becomes interactive, and
/// no status code shows that; the suite proved the circuit survives it before the header was flipped.
/// </para>
/// </remarks>
public sealed class SecurityHeadersMiddleware(
    RequestDelegate next,
    IWebHostEnvironment environment,
    IdentitySettings identity)
{
    /// <summary>The policy is enforced; the browser refuses what the policy does not admit.</summary>
    private const string HeaderName = "Content-Security-Policy";

    /// <summary>The policy. <c>'self'</c>, two named exceptions, and no <c>unsafe-inline</c> anywhere.</summary>
    internal const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "base-uri 'self'; " +
        "object-src 'none'; " +
        "frame-ancestors 'none'; " +
        "form-action 'self'; " +
        "script-src 'self'; " +
        "style-src 'self'; " +
        "img-src 'self' data:; " +
        "font-src 'self'; " +
        "connect-src 'self'; " +
        "upgrade-insecure-requests";

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Set before the response starts, so they ride on static assets as well as on pages.
        context.Response.Headers[HeaderName] = Policy(context);

        // A browser that guesses a response's type can be talked into running what it was told was
        // data. The application serves images, stylesheets, fonts and scripts, and none of them is
        // meant to be anything else.
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";

        // The application has nothing to gain from telling another site where a customer came from,
        // and the orders carry access tokens in their query strings.
        context.Response.Headers["Referrer-Policy"] = "no-referrer";

        // None of these is used, so none is granted. Naming them is the point: the browser refuses
        // them for this origin rather than asking, and an injection cannot ask on the page's behalf.
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        await next(context).ConfigureAwait(false);
    }

    /// <summary>
    /// The strict policy, except for the development documentation pages, which cannot work under it.
    /// </summary>
    private string Policy(HttpContext context)
        => IsDocumentation(context) ? DocumentationPolicy() : ContentSecurityPolicy;

    private bool IsDocumentation(HttpContext context)
        => environment.IsDevelopment()
           && context.Request.Path.StartsWithSegments(DocumentationPath.RequestPath, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The policy the documentation page is served under, derived from the strict one rather than
    /// written beside it.
    /// </summary>
    /// <remarks>
    /// It differs in exactly two ways, and both are forced. swagger-ui builds its page from an inline
    /// script and inline styles, which the strict policy refuses - the page then renders blank, which is
    /// the failure this middleware exists to make visible rather than to create. And the
    /// authorization-code exchange is a fetch from the browser to the provider's token endpoint, which
    /// <c>connect-src</c> governs and refuses, so the flow would fail after the redirect. Deriving the
    /// exception by substitution means a directive added to the strict policy cannot be forgotten here.
    /// Framing, <c>object-src</c>, <c>base-uri</c> and <c>form-action</c> are untouched.
    /// </remarks>
    private string DocumentationPolicy()
    {
        var policy = ContentSecurityPolicy
            .Replace("script-src 'self'", "script-src 'self' 'unsafe-inline'", StringComparison.Ordinal)
            .Replace("style-src 'self'", "style-src 'self' 'unsafe-inline'", StringComparison.Ordinal);

        return ProviderOrigin is { } origin
            ? policy.Replace("connect-src 'self'", $"connect-src 'self' {origin}", StringComparison.Ordinal)
            : policy;
    }

    /// <summary>The provider the page must reach to exchange its code, when the deployment names one.</summary>
    private string? ProviderOrigin
    {
        get
        {
            var authority = identity.Authority
                ?? (identity.Domain is { Length: > 0 } domain ? $"https://{domain}" : null);

            return authority is not null && Uri.TryCreate(authority, UriKind.Absolute, out var uri)
                ? uri.GetLeftPart(UriPartial.Authority)
                : null;
        }
    }
}
