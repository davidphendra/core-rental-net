using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Infrastructure.Sqlite;
using CoreRentalNet.Modules.Rentals.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The schema is applied at start-up wherever the application runs, not only in development.
/// </summary>
/// <remarks>
/// The failure this proves gone: a start outside development created the SQLite file but applied no
/// migrations, so the scheduler's first pass failed on <c>no such table: Rentals_Rental</c>. The test asks
/// the Rentals context a question only a migrated file can answer, so a missing table fails here rather
/// than as a log line after the process is up.
/// </remarks>
public sealed class DatabaseStartupTests : IDisposable
{
    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), $"core-rental-migrations-{Guid.NewGuid():N}");

    [Fact]
    public void A_start_outside_development_applies_the_rentals_schema()
    {
        var database = Path.Combine(_directory, "corerental.db");

        using (var host = new NonDevelopmentHostFactory(database))
        {
            // The host has to be pointed at the throwaway file, or the test would read the developer's
            // own database and prove nothing.
            host.Services.GetRequiredService<SqliteDatabaseSettings>().DatabasePath.Should().Be(database);

            using var scope = host.Services.CreateScope();
            var rentals = scope.ServiceProvider.GetRequiredService<RentalsContext>();

            rentals.Rentals.Count().Should().Be(
                0,
                "the table exists, so the first query runs instead of failing on a missing table");
        }
    }

    public void Dispose()
    {
        if (!Directory.Exists(_directory))
        {
            return;
        }

        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
            // Best effort: a leftover temp directory is not worth failing a run over.
        }
    }
}
