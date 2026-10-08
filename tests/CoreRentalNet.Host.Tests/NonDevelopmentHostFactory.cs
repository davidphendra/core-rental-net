using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The real application, started in an environment that is not development, against a throwaway file.
/// </summary>
/// <remarks>
/// Development is exactly the environment the migration guard keyed off, so a factory that used it could
/// not tell the fixed behaviour from the broken one. Identity is off by configuration, as in the
/// catalogue's own factory, so a machine holding real Auth0 settings cannot turn the gate live.
/// </remarks>
public sealed class NonDevelopmentHostFactory : WebApplicationFactory<Program>
{
    private readonly string _database;

    public NonDevelopmentHostFactory(string database) => _database = database;

    protected override IHost CreateHost(IHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Host configuration, not app configuration: the entry point reads the database path while it
        // is still composing the builder, before the host is built, so a source added later is too late.
        builder.ConfigureHostConfiguration(configuration => configuration.AddInMemoryCollection(
        [
            new KeyValuePair<string, string?>("Auth0:Enabled", "false"),
            new KeyValuePair<string, string?>("Sqlite:DatabasePath", _database),
        ]));

        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Testing");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        // The schema is a real file and the host pooled a connection to it, so the pool is emptied before
        // the files are removed rather than leaving a locked file behind on a platform that locks.
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        foreach (var file in new[] { _database, $"{_database}-wal", $"{_database}-shm" })
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }
}
