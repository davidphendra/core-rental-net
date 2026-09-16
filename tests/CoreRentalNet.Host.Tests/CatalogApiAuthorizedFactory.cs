using CoreRentalNet.Host.Infrastructure;
using CoreRentalNet.Host.Presentation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The application with identity configured, so the catalogue API requires a token.
/// </summary>
/// <remarks>
/// <para>
/// The composition root reads identity settings while <c>Program</c> runs - before a test can add
/// configuration - so identity is made configured here, where the container is final: the two
/// singletons the requirement handler reads are replaced, and the policy is re-pointed at
/// <see cref="CatalogApiTestHandler"/> so a principal can be produced without minting a JWT.
/// </para>
/// <para>
/// The environment is not Development, for the reason <see cref="CatalogApiFactory"/> gives: that
/// environment loads <c>appsettings.Local.json</c> and the developer's user secrets, which would
/// decide identity for the suite instead of the suite deciding for itself.
/// </para>
/// </remarks>
public sealed class CatalogApiAuthorizedFactory : WebApplicationFactory<Program>
{
    private const string ClaimType = "permissions";
    private const string ClaimValue = "read:catalog";

    private readonly string _database =
        Path.Combine(Path.GetTempPath(), $"core-rental-api-auth-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
        [
            new KeyValuePair<string, string?>("Sqlite:DatabasePath", _database),
        ]));

        builder.ConfigureTestServices(services =>
        {
            var identity = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth0:Enabled"] = "true",
                    ["Auth0:Domain"] = "tenant.example",
                    ["Auth0:ClientId"] = "client",
                    ["Authorization:CatalogRead:ClaimType"] = ClaimType,
                    ["Authorization:CatalogRead:ClaimValue"] = ClaimValue,
                })
                .Build();

            services.AddSingleton(IdentitySettings.From(identity));
            services.AddSingleton(CatalogReadClaim.From(identity));

            services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, CatalogApiTestHandler>(
                CatalogApiTestHandler.SchemeName,
                _ => { });

            services.PostConfigure<AuthorizationOptions>(options => options.AddPolicy(
                CatalogApiPolicy.Name,
                policy =>
                {
                    policy.AddRequirements(new CatalogReadRequirement());
                    policy.AddAuthenticationSchemes(CatalogApiTestHandler.SchemeName);
                }));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        foreach (var file in new[] { _database, $"{_database}-wal", $"{_database}-shm" })
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }
}
