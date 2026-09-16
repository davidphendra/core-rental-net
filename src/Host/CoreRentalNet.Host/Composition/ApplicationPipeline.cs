using CoreRentalNet.Host.Controllers;
using CoreRentalNet.Host.Infrastructure;
using CoreRentalNet.Host.Presentation;

namespace CoreRentalNet.Host.Composition;

/// <summary>
/// The order the request passes through. Order matters here and nowhere else, so it is stated once.
/// </summary>
internal static class ApplicationPipeline
{
    public static void UseApplicationPipeline(this WebApplication app, IdentitySettings identity)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(identity);

        ConfigureTransport(app);

        // First, so every response carries the policy - the pages and the assets behind them alike.
        // What the policy is, and why it is written strictly, is stated once in the middleware.
        app.UseMiddleware<SecurityHeadersMiddleware>();
        UseRevalidatingStaticFiles(app);

        // Authorization answers every request, not only the ones with a provider: the catalog policy
        // has something to say when identity is unconfigured too, which is that there is no reader to
        // check. Authentication is still registered only when there is somewhere to authenticate.
        if (identity.IsConfigured)
        {
            app.UseAuthentication();
        }

        app.UseAuthorization();

        // Mapped always, provider or not: with no provider the catalog policy opens the endpoint,
        // exactly as it opens the store page, and with one the request is refused before it arrives.
        CatalogController.MapEndpoints(app);

        if (identity.IsConfigured)
        {
            // Mapped only when there is an identity provider, so an unconfigured deployment has no
            // account routes at all.
            AccountController.MapEndpoints(app);
        }

        app.UseMiddleware<DraftTokenMiddleware>();
        app.UseAntiforgery();
    }

    private static void ConfigureTransport(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            return;
        }

        app.UseExceptionHandler("/error", createScopeForErrors: true);
        app.UseHsts();
        // HSTS tells a browser to come back over HTTPS; this sends it there the first time. A
        // deployment with no HTTPS port configured logs a warning and does not redirect, so this
        // cannot turn a working host into a loop.
        app.UseHttpsRedirection();
    }

    private static void UseRevalidatingStaticFiles(WebApplication app)
        // Revalidate, always. Without this the response carries an ETag and a Last-Modified but no
        // Cache-Control, so a browser applies heuristic freshness - a fraction of the file's age - and
        // serves a stylesheet it holds without asking whether it changed. That is a real hour of
        // confusion: a fix to slots.css was reported as not working while the browser was handing back
        // the previous copy. No-cache does not mean no caching; it means ask first, and a 304 costs
        // almost nothing on an application with six fonts and two scripts.
        => app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = context =>
                context.Context.Response.Headers.CacheControl = "no-cache",
        });
}
