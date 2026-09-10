using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CoreRentalNet.BuildingBlocks.Infrastructure.Sqlite;

/// <summary>
/// Applies the SQLite pragmas to every connection EF opens.
/// </summary>
/// <remarks>
/// The journal mode is persistent in the file, but the busy timeout is per connection, so
/// setting it once at startup would silently leave every later connection without it. That
/// is the difference between a locked database retrying and an intermittent failure.
/// </remarks>
public sealed class SqlitePragmaInterceptor(SqliteDatabaseSettings settings) : DbConnectionInterceptor
{
    private readonly SqliteDatabaseSettings settings = settings ?? throw new ArgumentNullException(nameof(settings));

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        Apply(connection);
        base.ConnectionOpened(connection, eventData);
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await ApplyAsync(connection, cancellationToken).ConfigureAwait(false);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken).ConfigureAwait(false);
    }

    private void Apply(DbConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = settings.PragmaScript;
        command.ExecuteNonQuery();
    }

    private async Task ApplyAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = settings.PragmaScript;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
