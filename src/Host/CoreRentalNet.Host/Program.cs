using CoreRentalNet.BuildingBlocks.Infrastructure.Sqlite;
using CoreRentalNet.Host.Components;
using CoreRentalNet.Host.Infrastructure;
using CoreRentalNet.Host.Presentation;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Application.Queries;
using CoreRentalNet.Modules.Catalog.Domain;
using CoreRentalNet.Modules.Catalog.Infrastructure;
using CoreRentalNet.Modules.Catalog.Infrastructure.Loading;
using CoreRentalNet.Modules.Workspace.Application.Commands;
using CoreRentalNet.Modules.Workspace.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Application.Queries;
using CoreRentalNet.Modules.Workspace.Domain;
using CoreRentalNet.Modules.Workspace.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);

// ---------------------------------------------------------------------------
// Catalog: read-only, loaded once from the catalog file. It has no database.
// ---------------------------------------------------------------------------
var catalogPath = builder.Configuration["Catalog:FilePath"];

if (string.IsNullOrWhiteSpace(catalogPath))
{
    catalogPath = Path.Combine(AppContext.BaseDirectory, "Catalog", "products.json");
}

var catalog = CatalogLoader.LoadFromFile(catalogPath, builder.Environment.WebRootPath);

builder.Services.AddSingleton<IProductCatalog>(catalog);
builder.Services.AddSingleton<IDefineProductPrices>(new DefineProductPrices(catalog));

// ---------------------------------------------------------------------------
// Workspace: one SQLite file. Path and journal mode come from configuration so
// moving to App Service stays a settings change (ADR-0014).
// ---------------------------------------------------------------------------
var configuredDatabasePath = builder.Configuration["Sqlite:DatabasePath"] ?? "App_Data/corerental.db";
var databasePath = Path.IsPathRooted(configuredDatabasePath)
    ? configuredDatabasePath
    : Path.Combine(builder.Environment.ContentRootPath, configuredDatabasePath);

var sqliteSettings = new SqliteDatabaseSettings(
    databasePath,
    builder.Configuration["Sqlite:JournalMode"] ?? "WAL",
    builder.Configuration.GetValue("Sqlite:BusyTimeoutSeconds", 30));

// Create the database directory and stamp the journal mode into the file, which is a
// persistent property of the file. EF's connection string does not create directories, and
// per-connection pragmas are applied by the interceptor below.
using (SqliteDatabase.Open(sqliteSettings))
{
}

builder.Services.AddSingleton(sqliteSettings);
builder.Services.AddSingleton<SqlitePragmaInterceptor>();

builder.Services.AddDbContext<WorkspaceContext>((provider, options) => options
    .UseSqlite(sqliteSettings.ConnectionString)
    .AddInterceptors(provider.GetRequiredService<SqlitePragmaInterceptor>()));

builder.Services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
builder.Services.AddScoped<IDefineWorkspaceComposition, DefineWorkspaceComposition>();

builder.Services.AddSingleton<CheckoutSettings>();
builder.Services.AddScoped<GetFeaturedProductsHandler>();
builder.Services.AddScoped<GetCatalogPageHandler>();
builder.Services.AddScoped<GetCatalogGroupHandler>();
builder.Services.AddScoped<GetWorkspaceHandler>();
builder.Services.AddScoped<GetWorkspaceQuoteHandler>();
builder.Services.AddScoped<StartDraftHandler>();
builder.Services.AddScoped<AssignProductHandler>();
builder.Services.AddScoped<RemoveAssignmentHandler>();
builder.Services.AddScoped<ChangeQuantityHandler>();
builder.Services.AddScoped<SetDeliveryAddressHandler>();

builder.Services.AddScoped<WorkspaceSession>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStaticFiles();
app.UseMiddleware<DraftTokenMiddleware>();
app.UseAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<WorkspaceContext>();
    await context.Database.MigrateAsync();
}

await app.RunAsync();
