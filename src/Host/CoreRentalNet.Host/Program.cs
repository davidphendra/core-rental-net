using CoreRentalNet.Host.Components;
using CoreRentalNet.Host.Composition;
using CoreRentalNet.Host.Presentation;

// The composition root, and nothing else: what the application is made of, in the order it is
// built, with each concern in its own file under Composition/.
var builder = WebApplication.CreateBuilder(args);

builder.AddLocalDevelopmentSettings();

var identity = builder.AddOptionalIdentity();
builder.AddPresentation();

builder.AddSqliteDatabase();
builder.AddCatalog();
builder.AddWorkspace();
builder.AddRentals();

builder.Services.AddScoped<WorkspaceSession>();

var app = builder.Build();

app.UseApplicationPipeline(identity);

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

await app.ApplyMigrationsInDevelopmentAsync();

await app.RunAsync();
