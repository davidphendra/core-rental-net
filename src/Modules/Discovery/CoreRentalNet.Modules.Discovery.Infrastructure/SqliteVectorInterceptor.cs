using System.Data.Common;
using CoreRentalNet.BuildingBlocks.Infrastructure.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CoreRentalNet.Modules.Discovery.Infrastructure;

/// <summary>
/// Loads the <c>vec0</c> extension on every connection EF opens for this store.
/// </summary>
/// <remarks>
/// <para>
/// <b>A migration connection needs the extension as much as a query connection does.</b> The vector
/// table can only be created, and the stored vectors only inserted, by a connection that has
/// <c>vec0</c> loaded; without this, applying the migration fails with "no such module" on a database
/// that is otherwise fine.
/// </para>
/// <para>
/// <b>It is registered for this context alone.</b> Only the vector store has a virtual table, and
/// loading a native extension on the other modules' ordinary connections would let a missing vec0
/// binary break code that has nothing to do with vectors.
/// </para>
/// </remarks>
public sealed class SqliteVectorInterceptor : DbConnectionInterceptor
{
    /// <inheritdoc />
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        SqliteDatabase.LoadVectorExtension(connection);
        base.ConnectionOpened(connection, eventData);
    }

    /// <inheritdoc />
    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        SqliteDatabase.LoadVectorExtension(connection);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken).ConfigureAwait(false);
    }
}
