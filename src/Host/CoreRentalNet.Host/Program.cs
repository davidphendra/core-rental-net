using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.BuildingBlocks.Infrastructure.Sqlite;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
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
using CoreRentalNet.Modules.Rentals.Application;
using CoreRentalNet.Modules.Rentals.Application.Checkout;
using CoreRentalNet.Modules.Rentals.Application.Orders;
using CoreRentalNet.Modules.Rentals.Application.Queries;
using CoreRentalNet.Modules.Rentals.Application.Scheduling;
using CoreRentalNet.Modules.Rentals.Domain;
using CoreRentalNet.Modules.Rentals.Infrastructure;
using CoreRentalNet.Modules.Workspace.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Loaded last so it wins, and gitignored so a developer's own tenant details and secret never
// have to live in a tracked file. appsettings.Development.json stays as the empty template.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);

// ---------------------------------------------------------------------------
// Identity, which is optional and additive (ADR-0016). With no domain configured
// nothing below is registered, the sign-in affordance hides itself, and the
// application runs exactly as it did before identity existed.
// ---------------------------------------------------------------------------
var identitySettings = IdentitySettings.From(builder.Configuration);
builder.Services.AddSingleton(identitySettings);

if (identitySettings.IsConfigured)
{
    builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie(options =>
        {
            options.ExpireTimeSpan = TimeSpan.FromDays(14);
            options.SlidingExpiration = true;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            // Over plain HTTP the cookie cannot be marked Secure, so local development is
            // inherently weaker than production. Production must be HTTPS.
            options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
        })
        .AddOpenIdConnect(options =>
        {
            options.Authority = $"https://{identitySettings.Domain}/";
            options.ClientId = identitySettings.ClientId!;
            options.ClientSecret = identitySettings.ClientSecret;
            options.ResponseType = "code";
            options.UsePkce = true;

            // The default scope for this provider omits the address we record on an order.
            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");

            options.CallbackPath = AccountEndpoints.CallbackPath;

            // Where the provider returns the browser after signing out. The handler then sends it
            // on to the application. The default is /signout-callback-oidc, which has to be
            // registered in the tenant just the same; naming it here keeps every identity route
            // under /account and makes the URL to register obvious.
            options.SignedOutCallbackPath = AccountEndpoints.SignedOutCallbackPath;

            // No access token and no refresh token are stored (ADR-0019). The profile is read once
            // at sign-in so the name and email on an order are reliable.
            options.SaveTokens = false;
            options.GetClaimsFromUserInfoEndpoint = true;

            options.TokenValidationParameters.NameClaimType = "name";

        });
}

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

builder.Services.AddDbContext<WorkspaceContext>((provider, options) =>
{
    WorkspacePersistence.Configure(options, sqliteSettings);
    options.AddInterceptors(provider.GetRequiredService<SqlitePragmaInterceptor>());
});

builder.Services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
builder.Services.AddScoped<IDefineWorkspaceComposition, DefineWorkspaceComposition>();

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

// ---------------------------------------------------------------------------
// Rentals: the same file, its own tables and its own migration history.
// ---------------------------------------------------------------------------
builder.Services.AddDbContext<RentalsContext>((provider, options) =>
{
    RentalsPersistence.Configure(options, sqliteSettings);
    options.AddInterceptors(provider.GetRequiredService<SqlitePragmaInterceptor>());
});

builder.Services.AddSingleton(new RentalsSettings(
    Money.Idr(builder.Configuration.GetValue("Rentals:DeliveryFeeAmount", 750_000m)),
    builder.Configuration.GetValue("Rentals:TaxRate", 0m)));

builder.Services.AddScoped<IRentalRepository, RentalRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IUnitOfWork, RentalsUnitOfWork>();
builder.Services.AddScoped<INumberSequence, SqliteNumberSequence>();
builder.Services.AddScoped<IPlaceOrder, PlaceOrderService>();
builder.Services.AddScoped<IConvertWorkspaceToOrder, ConvertWorkspaceToOrder>();
builder.Services.AddScoped<ICheckout, CheckoutService>();
builder.Services.AddScoped<IRunRentalSchedule, RentalScheduler>();
builder.Services.AddSingleton(SchedulerSettings.FromMinutes(
    builder.Configuration.GetValue("Rentals:SchedulerIntervalMinutes", 60)));
builder.Services.AddHostedService<RenewalSchedulerHostedService>();

builder.Services.AddScoped<GetRentalByTokenHandler>();
builder.Services.AddScoped<GetInvoicesByTokenHandler>();

builder.Services.AddScoped<WorkspaceSession>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStaticFiles();

if (identitySettings.IsConfigured)
{
    app.UseAuthentication();
    app.UseAuthorization();

    // Mapped only when there is an identity provider, so an unconfigured deployment has no
    // account routes at all.
    AccountEndpoints.Map(app);
}

app.UseMiddleware<DraftTokenMiddleware>();
app.UseAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var workspace = scope.ServiceProvider.GetRequiredService<WorkspaceContext>();
    await workspace.Database.MigrateAsync();

    var rentals = scope.ServiceProvider.GetRequiredService<RentalsContext>();
    await rentals.Database.MigrateAsync();
}

await app.RunAsync();
