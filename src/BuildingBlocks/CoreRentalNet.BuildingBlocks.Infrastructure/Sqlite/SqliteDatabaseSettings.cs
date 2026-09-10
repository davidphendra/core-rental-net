using CoreRentalNet.BuildingBlocks.Domain;
using Microsoft.Data.Sqlite;

namespace CoreRentalNet.BuildingBlocks.Infrastructure.Sqlite;

/// <summary>
/// Where the database file lives and how SQLite is configured.
/// </summary>
/// <remarks>
/// Both values are configuration rather than constants on purpose. The journal mode in
/// particular must change when this is deployed: SQLite's own documentation states that
/// <c>WAL does not work over a network filesystem</c>, and Azure App Service's only
/// persistent writable storage is a UNC share (ADR-0014).
/// </remarks>
public sealed record SqliteDatabaseSettings
{
    private static readonly string[] AllowedJournalModes =
    [
        "DELETE", "TRUNCATE", "PERSIST", "MEMORY", "WAL", "OFF",
    ];

    public SqliteDatabaseSettings(string databasePath, string journalMode = "WAL", int busyTimeoutSeconds = 30)
    {
        DatabasePath = Guard.NotEmpty(databasePath, "Database path", 500);

        var mode = journalMode.Trim().ToUpperInvariant();

        if (!AllowedJournalModes.Contains(mode, StringComparer.Ordinal))
        {
            throw new DomainRuleViolationException(
                $"'{journalMode}' is not a SQLite journal mode. Expected one of: {string.Join(", ", AllowedJournalModes)}.");
        }

        if (busyTimeoutSeconds < 0)
        {
            throw new DomainRuleViolationException("The busy timeout cannot be negative.");
        }

        JournalMode = mode;
        BusyTimeoutSeconds = busyTimeoutSeconds;
    }

    public string DatabasePath { get; }

    public string JournalMode { get; }

    public int BusyTimeoutSeconds { get; }

    /// <summary>WAL and a long busy timeout are correct on local disk; App Service needs a rollback journal.</summary>
    public static SqliteDatabaseSettings Local(string databasePath) => new(databasePath);

    public string ConnectionString => new SqliteConnectionStringBuilder
    {
        DataSource = DatabasePath,
        ForeignKeys = true,
    }.ToString();
}
