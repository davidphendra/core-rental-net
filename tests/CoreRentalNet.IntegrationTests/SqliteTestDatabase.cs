using CoreRentalNet.BuildingBlocks.Infrastructure.Sqlite;
using CoreRentalNet.Modules.Rentals.Infrastructure;
using CoreRentalNet.Modules.Workspace.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CoreRentalNet.IntegrationTests;

/// <summary>
/// A real SQLite file in a throwaway directory, migrated and deleted afterwards. Every test
/// gets its own, so no test can see another's state and a shared dev database is never touched.
/// </summary>
internal sealed class SqliteTestDatabase : IAsyncDisposable
{
    private readonly string directory;

    public SqliteTestDatabase()
    {
        directory = Path.Combine(Path.GetTempPath(), $"core-rental-tests-{Guid.NewGuid():N}");
        Settings = SqliteDatabaseSettings.Local(Path.Combine(directory, "test.db"));

        // Opening once applies the pragmas. WAL is a persistent property of the file.
        using var connection = SqliteDatabase.Open(Settings);
    }

    public SqliteDatabaseSettings Settings { get; }

    public WorkspaceContext CreateContext()
    {
        // Configured through the module's own settings, exactly as the host does, so the tests
        // cannot drift from the application.
        var options = new DbContextOptionsBuilder<WorkspaceContext>();
        WorkspacePersistence.Configure(options, Settings);

        return new WorkspaceContext(options.Options);
    }

    public async Task<WorkspaceContext> CreateMigratedContextAsync()
    {
        var context = CreateContext();
        await context.Database.MigrateAsync();
        return context;
    }

    public RentalsContext CreateRentalsContext()
    {
        var options = new DbContextOptionsBuilder<RentalsContext>();
        RentalsPersistence.Configure(options, Settings);

        return new RentalsContext(options.Options);
    }

    public async Task<RentalsContext> CreateMigratedRentalsContextAsync()
    {
        var context = CreateRentalsContext();
        await context.Database.MigrateAsync();
        return context;
    }

    public async ValueTask DisposeAsync()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        await Task.Yield();

        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
