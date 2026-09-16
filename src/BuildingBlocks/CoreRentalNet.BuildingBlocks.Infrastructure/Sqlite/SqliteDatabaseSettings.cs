using System.Collections.Immutable;
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
/// persistent writable storage is a UNC share.
/// </remarks>
public sealed record SqliteDatabaseSettings
{
    private static readonly ImmutableArray<string> s_allowedJournalModes =
    [
        "DELETE", "TRUNCATE", "PERSIST", "MEMORY", "WAL", "OFF",
    ];

    /// <summary>Validates and freezes one configuration, so no caller can hand SQLite a bad pragma.</summary>
    /// <exception cref="DomainRuleViolationException">
    /// The path is blank, the journal mode is not one SQLite knows, or the timeout is negative.
    /// </exception>
    public SqliteDatabaseSettings(string databasePath, string journalMode = "WAL", int busyTimeoutSeconds = 30)
    {
        DatabasePath = Guard.NotEmpty(databasePath, "Database path", 500);

        var mode = journalMode.Trim().ToUpperInvariant();

        if (!s_allowedJournalModes.Contains(mode, StringComparer.Ordinal))
        {
            throw new DomainRuleViolationException(
                $"'{journalMode}' is not a SQLite journal mode. Expected one of: {string.Join(", ", s_allowedJournalModes)}.");
        }

        if (busyTimeoutSeconds < 0)
        {
            throw new DomainRuleViolationException("The busy timeout cannot be negative.");
        }

        JournalMode = mode;
        BusyTimeoutSeconds = busyTimeoutSeconds;
    }

    /// <summary>The absolute or relative path of the single database file.</summary>
    public string DatabasePath { get; }

    /// <summary>The normalised, upper-case journal mode the pragma script will set.</summary>
    public string JournalMode { get; }

    /// <summary>How long a connection waits for a lock before it gives up, in seconds.</summary>
    public int BusyTimeoutSeconds { get; }

    /// <summary>WAL and a long busy timeout are correct on local disk; App Service needs a rollback journal.</summary>
    public static SqliteDatabaseSettings Local(string databasePath) => new(databasePath);

    /// <summary>One source for the pragma statements, used by both the opener and the EF interceptor.</summary>
    public string PragmaScript => $"PRAGMA journal_mode={JournalMode}; PRAGMA busy_timeout={BusyTimeoutSeconds * 1000};";

    /// <summary>The connection string for this file, with foreign keys enforced.</summary>
    public string ConnectionString => new SqliteConnectionStringBuilder
    {
        DataSource = DatabasePath,
        ForeignKeys = true,
    }.ToString();
}
