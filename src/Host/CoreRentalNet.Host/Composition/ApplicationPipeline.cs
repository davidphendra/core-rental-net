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

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/error", createScopeForErrors: true);
            app.UseHsts();
        }

        // Revalidate, always. Without this the response carries an ETag and a Last-Modified but no
        // Cache-Control, so a browser applies heuristic freshness - a fraction of the file's age - and
        // serves a stylesheet it holds without asking whether it changed. That is a real hour of
        // confusion: a fix to slots.css was reported as not working while the browser was handing back
        // the previous copy. No-cache does not mean no caching; it means ask first, and a 304 costs
        // almost nothing on an application with six fonts and two scripts.
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = context =>
                context.Context.Response.Headers.CacheControl = "no-cache",
        });

        if (identity.IsConfigured)
        {
            app.UseAuthentication();
            app.UseAuthorization();

            // Mapped only when there is an identity provider, so an unconfigured deployment has no
            // account routes at all.
            AccountController.MapEndpoints(app);
        }

        app.UseMiddleware<TokenEventHandlerMiddleware>();
        app.UseAntiforgery();
    }
}
