using CoreRentalNet.BuildingBlocks.Infrastructure.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CoreRentalNet.Modules.Discovery.Infrastructure;

/// <summary>
/// How this module's slice of the database is configured. One place, so the runtime and the
/// design-time tooling cannot disagree.
/// </summary>
public static class DiscoveryPersistence
{
    /// <summary>
    /// Each module keeps its own migration history in the shared file. Sharing EF's default table
    /// would mean two modules writing bookkeeping into one table, which is the kind of coupling
    /// the module boundary exists to prevent.
    /// </summary>
    public const string MigrationsHistoryTable = "Discovery_MigrationsHistory";

    public static void Configure(DbContextOptionsBuilder options, SqliteDatabaseSettings settings)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(settings);

        options.UseSqlite(settings.ConnectionString, sqlite => sqlite.MigrationsHistoryTable(MigrationsHistoryTable));
    }
}
