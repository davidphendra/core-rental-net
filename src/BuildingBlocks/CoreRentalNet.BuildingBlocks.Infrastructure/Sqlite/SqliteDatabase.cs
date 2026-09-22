using System.Globalization;
using Microsoft.Data.Sqlite;

namespace CoreRentalNet.BuildingBlocks.Infrastructure.Sqlite;

/// <summary>
/// Opens and configures a SQLite file.
/// </summary>
/// <remarks>
/// One writer at a time and concurrent requests are facts of life here, so the pragmas are
/// applied on every connection rather than left to defaults: without them a busy database
/// surfaces as an intermittent failure that looks like flaky code.
/// </remarks>
public static class SqliteDatabase
{
    /// <summary>The name of the vec0 extension, as SQLite resolves it against the app's native folders.</summary>
    private const string VectorExtension = "vec0";

    /// <summary>An open connection with the settings' pragmas already applied, and its directory created.</summary>
    public static SqliteConnection Open(SqliteDatabaseSettings settings)
        => Open(settings, loadVectorExtension: false);

    /// <summary>
    /// An open connection that can search vectors: the same as <see cref="Open"/>, with vec0 loaded.
    /// </summary>
    /// <remarks>
    /// <b>Only the vector paths ask for this.</b> Almost every connection in the application is an ordinary
    /// SQLite file with nothing to do with vectors, and loading a native extension on those would make a
    /// missing vec0 binary break the modules that do not use it. The tool that writes the vectors and the
    /// adapter that searches them opt in; nothing else does.
    /// </remarks>
    public static SqliteConnection OpenWithVectors(SqliteDatabaseSettings settings)
        => Open(settings, loadVectorExtension: true);

    private static SqliteConnection Open(SqliteDatabaseSettings settings, bool loadVectorExtension)
    {
        ArgumentNullException.ThrowIfNull(settings);

        EnsureDirectoryExists(settings.DatabasePath);

        var connection = new SqliteConnection(settings.ConnectionString);
        connection.Open();
        ApplyPragmas(connection, settings);

        if (loadVectorExtension)
        {
            // Microsoft.Data.Sqlite enables extension loading for the call itself; the name resolves against
            // the native folders the package copied vec0 into.
            connection.LoadExtension(VectorExtension);
        }

        return connection;
    }

    /// <summary>Runs the settings' pragma script against an already-open connection.</summary>
    public static void ApplyPragmas(SqliteConnection connection, SqliteDatabaseSettings settings)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(settings);

        using var command = connection.CreateCommand();

        command.CommandText = settings.PragmaScript;
        command.ExecuteNonQuery();
    }

    /// <summary>Reads back the journal mode, so a caller can assert what the pragma actually set.</summary>
    public static string ReadJournalMode(SqliteConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode;";

        return (command.ExecuteScalar() as string ?? string.Empty).ToUpperInvariant();
    }

    /// <summary>Reads back the busy timeout in milliseconds, which is per connection rather than per file.</summary>
    public static int ReadBusyTimeoutMilliseconds(SqliteConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA busy_timeout;";

        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    private static void EnsureDirectoryExists(string databasePath)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(databasePath));

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
