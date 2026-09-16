using CoreRentalNet.Host.Presentation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The same application, with a public client named for the documentation page.
/// </summary>
/// <remarks>
/// PKCE is documented for clients that cannot hold a secret, while the application's own client is a
/// confidential one, so a deployment may need a different client for this page. This factory proves
/// that naming one takes effect without disturbing the client the application signs people in with.
/// </remarks>
public sealed class SwaggerClientFactory : WebApplicationFactory<Program>
{
    private const string SwaggerClient = "public-client";

    private readonly string _database =
        Path.Combine(Path.GetTempPath(), $"core-rental-swagger-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
        [
            new KeyValuePair<string, string?>("Sqlite:DatabasePath", _database),
            new KeyValuePair<string, string?>("Swagger:ClientId", SwaggerClient),
        ]));

        builder.ConfigureTestServices(services =>
        {
            var identity = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth0:Enabled"] = "true",
                    ["Auth0:Domain"] = "tenant.example",
                    ["Auth0:ClientId"] = "sign-in-client",
                    ["Auth0:Audience"] = "https://catalogue.example",
                    ["Auth0:Scope"] = "openid profile email read:catalog",
                })
                .Build();

            services.AddSingleton(IdentitySettings.From(identity));
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
