using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using CoreRentalNet.Modules.Rentals.Domain.Numbering;
using CoreRentalNet.Modules.Rentals.Domain.Persistence;

namespace CoreRentalNet.Modules.Rentals.Infrastructure;

/// <summary>
/// Reserves numbers with a single atomic statement, executed directly on the connection.
/// </summary>
/// <remarks>
/// An insert-or-increment in one round trip means two checkouts racing each other cannot take the
/// same number, and there is no read-modify-write window to lose. It runs on the connection rather
/// than through EF's query pipeline, which only composes over SELECT statements.
/// <para>
/// A reservation commits immediately, so an order that then fails leaves a gap in the sequence.
/// Numbers only have to be unique and increasing, and a gap is much better than a duplicate.
/// </para>
/// </remarks>
public sealed class SqliteNumberSequence(RentalsContext context) : INumberSequence
{
    private const string ReserveSql =
        """
        INSERT INTO Rentals_NumberSequence (Kind, Year, Value) VALUES ($kind, $year, 1)
        ON CONFLICT (Kind, Year) DO UPDATE SET Value = Value + 1
        RETURNING Value;
        """;

    public async Task<int> ReserveNextAsync(SequenceKind kind, int year, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        await context.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        var connection = context.Database.GetDbConnection();

        await using var command = connection.CreateCommand();
        command.CommandText = ReserveSql;
        command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();

        AddParameter(command, "$kind", (int)kind);
        AddParameter(command, "$year", year);

        var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        return Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void AddParameter(DbCommand command, string name, int value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
