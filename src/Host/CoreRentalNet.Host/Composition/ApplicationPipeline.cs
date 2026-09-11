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

        app.UseStaticFiles();

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
