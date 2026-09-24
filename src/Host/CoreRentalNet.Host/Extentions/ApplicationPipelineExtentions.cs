using CoreRentalNet.Host.AiBuilder;
using CoreRentalNet.Host.Configs;
using CoreRentalNet.Host.Controllers;
using CoreRentalNet.Host.Infrastructure;
using CoreRentalNet.Host.Presentation;

namespace CoreRentalNet.Host.Extentions;

/// <summary>
/// The order the request passes through. Order matters here and nowhere else, so it is stated once.
/// </summary>
internal static class ApplicationPipelineExtentions
{
    public static void UseApplicationPipeline(this WebApplication app, IdentitySettings identity)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(identity);

        ConfigureTransport(app);

        // First, so every response carries the policy - the pages and the assets behind them alike.
        // What the policy is, and why it is written strictly, is stated once in the middleware.
        app.UseMiddleware<SecurityHeadersMiddleware>();

        UseApiResponses(app);
        UseRevalidatingStaticFiles(app);
        UseIdentity(app, identity);
        UseApplicationEndpoints(app, identity);

        app.UseMiddleware<DraftTokenMiddleware>();
        app.UseAntiforgery();
    }

    /// <summary>
    /// Every way the API can refuse, answered with one shape.
    /// </summary>
    /// <remarks>
    /// Scoped to the API on purpose. A browser navigating to a bad address must still be given a page,
    /// and Blazor has its own error UI for the circuit, so mounting this for the whole application
    /// would answer page requests with problem details - a worse defect than the one it fixes. Neither
    /// middleware writes the body: the problem-details service does, which is what supplies the trace
    /// id, the error code and the instance. The exception handler turns an unhandled failure into a
    /// 500 that can be reported, and the status-code pages give the otherwise-empty 401, 403, 404 and
    /// 405 something to read.
    /// </remarks>
    private static void UseApiResponses(WebApplication app)
        => app.UseWhen(
            context => context.Request.Path.StartsWithSegments(
                CatalogRoutes.Prefix,
                StringComparison.OrdinalIgnoreCase),
            api =>
            {
                api.UseExceptionHandler();
                api.UseStatusCodePages();
            });

    /// <summary>Authorization answers every request; authentication only where there is a provider.</summary>
    /// <remarks>
    /// The catalogService policy has something to say when identity is unconfigured too, which is that there
    /// is no reader to check.
    /// </remarks>
    private static void UseIdentity(WebApplication app, IdentitySettings identity)
    {
        if (identity.IsConfigured)
        {
            app.UseAuthentication();
        }

        app.UseAuthorization();
    }

    /// <summary>The routes the application answers, and the ones a deployment may not have.</summary>
    private static void UseApplicationEndpoints(WebApplication app, IdentitySettings identity)
    {
        // Mapped always, provider or not: with no provider the catalogService policy opens the endpoint,
        // exactly as it opens the store page, and with one the request is refused before it arrives.
        // Controllers are discovered, so a route added to a controller is reachable without a second
        // place to remember, and a gate left off an action is a failing test rather than a live hole.
        app.MapControllers();
        MapDevelopmentDocumentation(app);
        MapCatalogMcp(app);

        // The suggestion run: a minimal-API endpoint rather than a controller, because its answer is held open
        // and written to for as long as the run takes. It carries its own permission; a customer who never sees
        // the AI section must not be able to spend through a path they were not shown.
        app.MapSuggestionEndpoint();

        if (identity.IsConfigured)
        {
            // Mapped only when there is an identity provider, so an unconfigured deployment has no
            // account routes at all.
            AccountController.MapEndpoints(app);
        }
    }

    /// <summary>
    /// Publishes the OpenAPI document in development, and nowhere else.
    /// </summary>
    /// <remarks>
    /// The document names every route and shape the application has, so it is served only where
    /// whoever is writing a caller needs to read it. It is anonymous: a description of the API is not
    /// the API, so it carries no gate of its own.
    /// </remarks>
    private static void MapDevelopmentDocumentation(WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        app.MapOpenApi(DocumentationPath.Document);
        SwaggerUiRegistrationExtentions.MapSwaggerUi(app);
    }

    /// <summary>
    /// Maps the MCP endpoint the catalogue's tools are published over.
    /// </summary>
    /// <remarks>
    /// <b>The endpoint requires an authenticated caller and nothing more;</b> which tools that caller may use
    /// is decided per tool, by the same policies the REST endpoints carry. The gate is a named policy so a test
    /// can re-point it at its own scheme, exactly as the API's policies are re-pointed - a gate written inline
    /// here would be the one thing the hermetic suite could not exercise.
    /// </remarks>
    private static void MapCatalogMcp(WebApplication app)
        => app.MapMcp(McpRoutes.Path).RequireAuthorization(McpPolicy.Name);

    private static void ConfigureTransport(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            return;
        }

        // There is deliberately no exception handler for the pages here. The one that was configured
        // pointed at "/error", which nothing serves: an unhandled failure was answered 404 with an
        // empty body, which is the wrong status and no diagnosis at all. The API has its own handler
        // above. A page-shaped error page for the browser is a separate, larger piece of work - it
        // needs a component and a status code it can set - and is recorded rather than pretended to.
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
