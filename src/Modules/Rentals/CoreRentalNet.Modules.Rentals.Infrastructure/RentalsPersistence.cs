using CoreRentalNet.BuildingBlocks.Infrastructure.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CoreRentalNet.Modules.Rentals.Infrastructure;

/// <summary>How this module's slice of the database is configured. One place, no drift.</summary>
public static class RentalsPersistence
{
    public const string MigrationsHistoryTable = "Rentals_MigrationsHistory";

    public static void Configure(DbContextOptionsBuilder options, SqliteDatabaseSettings settings)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(settings);

        options.UseSqlite(settings.ConnectionString, sqlite => sqlite.MigrationsHistoryTable(MigrationsHistoryTable));
    }
}
