using CoreRentalNet.BuildingBlocks.Infrastructure.Sqlite;

namespace CoreRentalNet.Host.Composition;

/// <summary>
/// The one database file, and the settings that describe it.
/// </summary>
/// <remarks>
/// Registered before the modules that use it, because each of them resolves these settings when it
/// builds its own context. Path and journal mode come from configuration so that moving to a
/// deployed environment stays a settings change (ADR-0014).
/// </remarks>
internal static class SqliteRegistration
{
    public static void AddSqliteDatabase(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var configured = builder.Configuration["Sqlite:DatabasePath"] ?? "App_Data/corerental.db";

        var settings = new SqliteDatabaseSettings(
            Path.IsPathRooted(configured) ? configured : Path.Combine(builder.Environment.ContentRootPath, configured),
            builder.Configuration["Sqlite:JournalMode"] ?? "WAL",
            builder.Configuration.GetValue("Sqlite:BusyTimeoutSeconds", 30));

        // Opening once creates the directory and stamps the journal mode into the file, which is a
        // persistent property of it. EF's connection string does not create directories, and the
        // per-connection pragmas are applied by the interceptor instead.
        using (SqliteDatabase.Open(settings))
        {
        }

        builder.Services.AddSingleton(settings);
        builder.Services.AddSingleton<SqlitePragmaInterceptor>();
    }
}
