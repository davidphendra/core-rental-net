using Microsoft.Data.Sqlite;

namespace CoreRentalNet.CatalogIngestion.Tests.Storage;

/// <summary>Reads the recipe back with SQL, the way the application's freshness check does.</summary>
/// <remarks>
/// The table name is written out rather than taken from the shared constant, so this fails if the contract
/// moves on one side only. Null means the table exists and holds no row, which is the state a reader reports
/// as "never built".
/// </remarks>
internal static class StoredRecipes
{
    public static StoredRecipe? Read(string databasePath)
    {
        using var connection = new SqliteConnection(
            new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString());

        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT modelId, width, composition, catalogueHash, created_at FROM product_embedding_recipe;";

        using var reader = command.ExecuteReader();

        return reader.Read()
            ? new StoredRecipe(
                reader.GetString(0),
                reader.GetInt32(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4))
            : null;
    }
}
