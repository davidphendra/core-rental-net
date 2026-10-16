using CoreRentalNet.BuildingBlocks.Infrastructure.Sqlite;
using CoreRentalNet.BuildingBlocks.Infrastructure.Vectors;
using Microsoft.Data.Sqlite;

namespace CoreRentalNet.CatalogIngestion.Seeding;

/// <summary>Reads a seed out of a vector file the ingestion tool built.</summary>
/// <remarks>
/// <para>
/// <b>It finds the tables rather than assuming their names.</b> The file being lifted is the one written
/// before the store moved into the Discovery module, so its tables are still <c>product_embedding</c> and
/// <c>product_embedding_recipe</c>. Looking the name up in the file lets the same reader lift a file written
/// after the move, whose tables carry their new names.
/// </para>
/// <para>
/// <b>The vector column is read as its stored blob.</b> A <c>vec0</c> table returns the vector as the
/// little-endian float32 bytes it was given, so the seed can carry those exact bytes into SQL rather than
/// decode and re-encode them.
/// </para>
/// </remarks>
internal static class SqliteEmbeddingSeedReader
{
    /// <summary>The name the ingestion file used before the store moved into the Discovery module.</summary>
    private const string LegacyVectorTable = "product_embedding";

    /// <summary>The recipe table's name before the move.</summary>
    private const string LegacyRecipeTable = "product_embedding_recipe";

    /// <summary>The recipe and every stored passage, in the order they were written.</summary>
    public static EmbeddingSeed Read(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        using var connection = SqliteDatabase.OpenWithVectors(SqliteDatabaseSettings.Local(databasePath));

        var vectorTable = Resolve(connection, ProductVectorContract.VectorTable, LegacyVectorTable);
        var recipeTable = Resolve(connection, ProductVectorContract.RecipeTable, LegacyRecipeTable);

        return new EmbeddingSeed(ReadRecipe(connection, recipeTable), ReadVectors(connection, vectorTable));
    }

    /// <summary>Whichever of the two names the file actually holds.</summary>
    private static string Resolve(SqliteConnection connection, string preferred, string legacy)
    {
        foreach (var candidate in (ReadOnlySpan<string>)[preferred, legacy])
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name = $name;";
            command.Parameters.AddWithValue("$name", candidate);

            if (command.ExecuteScalar() is not null)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException(
            $"The file holds neither '{preferred}' nor '{legacy}', so it is not a vector file this tool wrote.");
    }

    /// <summary>The one row describing what built the vectors.</summary>
    private static SeedRecipe ReadRecipe(SqliteConnection connection, string table)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT modelId, width, composition, catalogueHash, created_at FROM {table} LIMIT 1;";

        using var reader = command.ExecuteReader();

        if (!reader.Read())
        {
            throw new InvalidOperationException(
                $"'{table}' holds no row, so the file does not say what built its vectors.");
        }

        return new SeedRecipe(
            reader.GetString(0),
            reader.GetInt32(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4));
    }

    /// <summary>Every stored passage, in the order the tool wrote it.</summary>
    private static IReadOnlyList<SeedVector> ReadVectors(SqliteConnection connection, string table)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT skuNo, embedding, id, name, description, created_at FROM {table} ORDER BY rowid;";

        using var reader = command.ExecuteReader();

        var vectors = new List<SeedVector>();

        while (reader.Read())
        {
            vectors.Add(new SeedVector(
                reader.GetString(0),
                Convert.ToHexString((byte[])reader.GetValue(1)),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5)));
        }

        // An empty file would write a seed that fills nothing, which is a smaller store than the catalogue
        // needs rather than a run with nothing to do.
        return vectors.Count > 0
            ? vectors
            : throw new InvalidOperationException($"'{table}' holds no vectors, so there is nothing to seed.");
    }
}
