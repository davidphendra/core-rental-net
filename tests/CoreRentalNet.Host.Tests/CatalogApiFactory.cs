using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The real application, in process, for the catalogue endpoint's HTTP tests.
/// </summary>
/// <remarks>
/// Identity is off by configuration, not by having no credentials: development loads
/// <c>appsettings.Local.json</c> last, and on a machine that holds real Auth0 settings the catalogue
/// gate would otherwise be live and every request answered 401. The database is a throwaway file, so
/// a run cannot touch <c>App_Data</c>.
/// </remarks>
public sealed class CatalogApiFactory : WebApplicationFactory<Program>
{
    private readonly string _database =
        Path.Combine(Path.GetTempPath(), $"core-rental-api-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
        [
            new KeyValuePair<string, string?>("Auth0:Enabled", "false"),
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
