using CoreRentalNet.BuildingBlocks.Infrastructure.Sqlite;
using CoreRentalNet.BuildingBlocks.Infrastructure.Vectors;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using Microsoft.Data.Sqlite;

namespace CoreRentalNet.Modules.Discovery.Infrastructure.Vectors;

/// <summary>
/// Searches the vectors the ingestion tool wrote, out of the tool's own file.
/// </summary>
/// <remarks>
/// <para>
/// <b>The nearest passages are found by SQLite, not by this class.</b> The tool's table is a <c>vec0</c> virtual
/// table and the search is a <c>MATCH</c> query; this class only binds the query vector, reads the rows back,
/// and reduces a product's several passages to its nearest one (<c>MIN(distance)</c>). There is no cosine and
/// no ordering computed here, because the extension is the one implementation of both.
/// </para>
/// <para>
/// <b>It reads a file this module does not own and never writes to it.</b> The tool owns the schema and the
/// names come from the shared contract, so a rename on either side fails here instead of silently returning
/// nothing.
/// </para>
/// <para>
/// <b>Every failure is the same fact to a caller</b> - the deployment cannot read the vectors right now - so
/// every failure arrives as <see cref="ProductSimilarityUnavailableException"/>. Cancellation is not caught:
/// a caller who stopped the request has not met an outage.
/// </para>
/// </remarks>
public sealed class SqliteProductSimilarityService : IProductSimilarityService
{
    /// <summary>How many of the nearest stored passages are asked for, before they are reduced to products.</summary>
    private const int NearestPassages = 10;

    private readonly string _databasePath;
    private readonly SqliteDatabaseSettings _settings;

    /// <summary>The search over one ingestion tool's vector file.</summary>
    public SqliteProductSimilarityService(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        _databasePath = databasePath;
        _settings = SqliteDatabaseSettings.Local(databasePath);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NearestProduct>> NearestAsync(
        float[] queryVector,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(queryVector);

        if (!File.Exists(_databasePath))
        {
            throw new ProductSimilarityUnavailableException(
                $"The vector file '{_databasePath}' does not exist, so the catalogue cannot be searched by similarity. "
                + "Run CoreRentalNet.CatalogIngestion to build it.");
        }

        try
        {
            return await SearchAsync(queryVector, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new ProductSimilarityUnavailableException(
                $"The stored catalogue vectors could not be searched, so the catalogue cannot be searched by similarity: {exception.Message}",
                exception);
        }
    }

    /// <summary>The nearest products to one query vector, as sqlite-vec ranks them.</summary>
    private async Task<IReadOnlyList<NearestProduct>> SearchAsync(
        float[] queryVector,
        CancellationToken cancellationToken)
    {
        using var connection = SqliteDatabase.OpenWithVectors(_settings);

        using var command = connection.CreateCommand();

        // The CTE is the KNN query: MATCH ranks by distance and k caps it. It is marked MATERIALIZED so the
        // GROUP BY below is applied to a finished result - vec0 refuses a KNN query the planner tries to
        // satisfy ordering for on any column but distance. The outer select then takes one row per product -
        // the passage nearest the request - because a product is stored as several.
        command.CommandText = $"""
            WITH nearest AS MATERIALIZED (
                SELECT skuNo, distance
                FROM {ProductVectorContract.VectorTable}
                WHERE embedding MATCH vec_f32($query) AND k = {NearestPassages}
            )
            SELECT skuNo, MIN(distance) AS distance
            FROM nearest
            GROUP BY skuNo
            ORDER BY distance ASC;
            """;

        command.Parameters.Add("$query", SqliteType.Blob).Value = VectorBlob.ToBytes(queryVector);

        var nearest = new List<NearestProduct>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            nearest.Add(new NearestProduct(reader.GetString(0), reader.GetDouble(1)));
        }

        return nearest;
    }
}
