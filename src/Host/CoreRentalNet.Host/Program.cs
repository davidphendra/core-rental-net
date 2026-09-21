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
// The AI section is gated on a permission the deployment configures, and is closed when it has not:
// no section, rather than an open one. The catalogue's own rule is untouched, and stays open.
builder.AddAiAuthorization();
builder.AddPresentation();

builder.AddSqliteDatabase();
builder.AddCatalog();
builder.AddCatalogApi();
builder.AddApiResponses();
builder.AddCatalogOpenApi();
builder.AddWorkspace();
builder.AddRentals();

// The catalogue index and the shortlist. It embeds through the same project the agent is reached through,
// with its own credential, and a deployment that has not been told where to embed gets a hidden feature
// rather than a failed request.
builder.AddDiscovery();

// The suggestion agent is reached through the Foundry client, and is unavailable rather than open when
// it has not been configured. Nothing else in the application knows that Foundry exists.
builder.AddSuggestionAgent();

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

// After the migrations, because in development those are what create the index's tables. An index that is absent
// or stale hides the AI feature and names the tool to run; it does not stop the application.
await app.GateSuggestionOnIndexAsync();

await app.RunAsync();
