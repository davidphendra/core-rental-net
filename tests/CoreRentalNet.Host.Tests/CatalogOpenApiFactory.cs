using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The application as development runs it, which is where the OpenAPI document is served.
/// </summary>
/// <remarks>
/// The document endpoint is anonymous, so the identity question that makes
/// <see cref="CatalogApiFactory"/> run in the Testing environment does not apply here: this factory
/// needs Development precisely because that is the environment the document is published in. The
/// database is a throwaway file, as in the other factories.
/// </remarks>
public sealed class CatalogOpenApiFactory : WebApplicationFactory<Program>
{
    private readonly string _database =
        Path.Combine(Path.GetTempPath(), $"core-rental-openapi-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
        [
            new KeyValuePair<string, string?>("Sqlite:DatabasePath", _database),
        ]));
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
