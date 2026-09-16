using CoreRentalNet.Host.Components;
using CoreRentalNet.Host.Composition;
using CoreRentalNet.Host.Presentation;

// The composition root, and nothing else: what the application is made of, in the order it is
// built, with each concern in its own file under Composition/.
var builder = WebApplication.CreateBuilder(args);

builder.AddLocalDevelopmentSettings();
// Before anything formats a value: see BusinessCulture for why the culture is set here and read
// from configuration rather than written beside each format string.
builder.AddBusinessCulture();
builder.AddBuildingBlocks();

var identity = builder.AddOptionalIdentity();
builder.AddCatalogAuthorization();
builder.AddCatalogApiAuthentication(identity);
builder.AddCatalogApiAuthorization(identity);
builder.AddPresentation();

builder.AddSqliteDatabase();
builder.AddCatalog();
builder.AddCatalogApi();
builder.AddWorkspace();
builder.AddRentals();

builder.Services.AddScoped<IWorkspaceSession, WorkspaceSession>();

var app = builder.Build();

app.UseApplicationPipeline(identity);

app.MapRazorComponents<App>()
    // The framework answers its component endpoints with a Content-Security-Policy carrying only this
    // one directive, and 'self' is the default. An app that is never framed refuses to be framed at
    // all, and saying so here keeps the framework's header and the application's own from disagreeing -
    // two policies are enforced together, so a disagreement is a subtle way to be wrong.
    .AddInteractiveServerRenderMode(options => options.ContentSecurityFrameAncestorsPolicy = "'none'");

await app.ApplyMigrationsInDevelopmentAsync();

await app.RunAsync();
