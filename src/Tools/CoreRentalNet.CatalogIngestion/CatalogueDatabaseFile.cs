using System.Globalization;
using System.Text.Json;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Catalog.Domain;
using Microsoft.Data.Sqlite;

namespace CoreRentalNet.CatalogIngestion;

/// <summary>Reads the catalogue rows the embedding run consumes from catalog.db.</summary>
/// <remarks>
/// The catalogue database is the input; this reader does not consult products.json and does not consult the
/// vector output. The vector output is a derived file and has no product rows.
/// </remarks>
internal static class CatalogueDatabaseFile
{
    public static IReadOnlyList<Product> Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Catalogue database not found: '{path}'.");
        }

        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadOnly,
        }.ToString());
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Sku, Name, Category, SubCategory, Amount, Currency, Description,
                   Image, IsFeatured, Tags, BestFor, NotFor, Attributes
            FROM Catalog_Product
            ORDER BY SortOrder, Sku;
            """;

        using var reader = command.ExecuteReader();
        var products = new List<Product>();

        while (reader.Read())
        {
            products.Add(ToProduct(reader));
        }

        return products;
    }

    private static Product ToProduct(SqliteDataReader row)
        => new(
            row.GetString(0),
            row.GetString(1),
            (ProductCategory)row.GetInt32(2),
            row.IsDBNull(3) ? null : (ProductSubCategory)row.GetInt32(3),
            new Money(decimal.Parse(row.GetString(4), CultureInfo.InvariantCulture), row.GetString(5)),
            row.GetString(6),
            new ProductMetadata(
                StringList(row.GetString(9)),
                Attributes(row.GetString(12)),
                StringList(row.GetString(10)),
                StringList(row.GetString(11))),
            row.GetString(7),
            row.GetBoolean(8),
            ImageAvailable: false);

    private static IReadOnlyList<string> StringList(string json)
        => JsonSerializer.Deserialize<List<string>>(json) ?? [];

    private static IReadOnlyDictionary<string, string> Attributes(string json)
        => JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
}
