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
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
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
        context.Response.Headers[HeaderName] = ContentSecurityPolicy;

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
}
