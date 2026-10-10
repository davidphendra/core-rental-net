using CoreRentalNet.Host.Components;
using CoreRentalNet.Host.Extentions;
using CoreRentalNet.Host.Presentation;

// The composition root, and nothing else: what the application is made of, in the order it is
// built, with each concern in its own file under Composition/.
var builder = WebApplication.CreateBuilder(args);

builder.AddLocalDevelopmentSettings();
// Before anything formats a value: see BusinessCultureExtentions for why the culture is set here and read
// from configuration rather than written beside each format string.
builder.AddBusinessCulture();
builder.AddBuildingBlocks();
// Telemetry before anything that emits it: the meter and the build identity are registered here, so an
// instrument recorded later is attributed to this deployment rather than to nothing.
builder.AddTelemetry();

var identity = builder.AddOptionalIdentity();
builder.AddCatalogAuthorization();
builder.AddCatalogApiAuthentication(identity);
builder.AddCatalogApiAuthorization(identity);
// Similarity search is a second capability on the same catalogue, with its own permission: a deployment
// may allow a caller to read the catalogue and not to search it by meaning, which embeds their sentence at
// a server. Closed when the deployment has not said which claim entitles a caller.
builder.AddSimilaritySearchAuthorization(identity);
builder.AddCatalogMcpAuthorization(identity);
// The suggestion panel is gated on a permission the deployment configures, and is closed when it has not:
// no panel, rather than an open one. The catalogue's own rule is untouched, and stays open.
builder.AddWorkspaceSuggestionAuthorization();
builder.AddPresentation();

builder.AddSqliteDatabase();
builder.AddCatalog();
builder.AddCatalogApi();
builder.AddCatalogMcp();
builder.AddApiResponses();
builder.AddCatalogOpenApi();
builder.AddWorkspace();
builder.AddRentals();

// The catalogue vectors. The vectors are built by the ingestion tool against a local
// OpenAI-compatible server, and this application embeds its customer queries through the same one and reads
// those vectors out of the tool's own file. A deployment that has not been told where either is gets a feature
// that reports itself unavailable rather than a failed request.
builder.AddDiscovery();

// The suggestion agent is reached through the Foundry client, and is unavailable rather than open when it has
// not been configured. Nothing else in the application knows that Foundry exists.
builder.AddWorkspaceSuggestions();

builder.Services.AddScoped<IWorkspaceSession, WorkspaceSession>();

var app = builder.Build();

// First, so a deployment that never becomes ready still says which build it is. The same four values the
// telemetry resource carries and the footer shows, on the log stream.
app.AnnounceBuildIdentity();

app.UseApplicationPipeline(identity);

app.MapRazorComponents<App>()
    // The framework answers its component endpoints with a Content-Security-Policy carrying only this
    // one directive, and 'self' is the default. An app that is never framed refuses to be framed at
    // all, and saying so here keeps the framework's header and the application's own from disagreeing -
    // two policies are enforced together, so a disagreement is a subtle way to be wrong.
    .AddInteractiveServerRenderMode(options => options.ContentSecurityFrameAncestorsPolicy = "'none'");

await app.ApplyMigrationsAsync();

// Vectors that are absent or stale refuse the similarity search and name the tool to run; they do not stop
// the application.
app.CheckSimilarityOnVectorEmbedding();

await app.RunAsync();
