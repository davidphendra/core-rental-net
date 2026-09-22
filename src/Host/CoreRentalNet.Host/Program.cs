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

var identity = builder.AddOptionalIdentity();
builder.AddCatalogAuthorization();
builder.AddCatalogApiAuthentication(identity);
builder.AddCatalogApiAuthorization(identity);
// Similarity search is a second capability on the same catalogue, with its own permission: a deployment
// may allow a caller to read the catalogue and not to search it by meaning, which embeds their sentence at
// a server. Closed when the deployment has not said which claim entitles a caller.
builder.AddSimilaritySearchAuthorization(identity);
builder.AddCatalogMcpAuthorization(identity);
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

// Vectors that are absent or stale refuse the similarity search and name the tool to run; they do not stop
// the application.
app.GateSimilarityOnIndex();

await app.RunAsync();
