using CoreRentalNet.BuildingBlocks.Infrastructure.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CoreRentalNet.Modules.Discovery.Infrastructure;

/// <summary>
/// How the vector store is configured. One place, so the runtime and the design-time tooling cannot
/// disagree.
/// </summary>
public static class EmbeddingPersistence
{
    /// <summary>
    /// The migration history for this store, kept apart from EF's default table so this module's
    /// bookkeeping is not written into another context's.
    /// </summary>
    public const string MigrationsHistoryTable = "Embedding_MigrationsHistory";

    public static void Configure(DbContextOptionsBuilder options, SqliteDatabaseSettings settings)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(settings);

        options.UseSqlite(settings.ConnectionString, sqlite => sqlite.MigrationsHistoryTable(MigrationsHistoryTable));
    }
}
