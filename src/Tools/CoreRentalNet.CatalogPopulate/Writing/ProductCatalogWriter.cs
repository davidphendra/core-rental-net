using System.Text.Encodings.Web;
using System.Text.Json;
using CoreRentalNet.CatalogPopulate.Catalog;

namespace CoreRentalNet.CatalogPopulate.Writing;

/// <summary>Writes products.json in the shape and order the application's loader reads.</summary>
internal static class ProductCatalogWriter
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>Writes the rows, creating the directory when needed.</summary>
    public static void Write(string path, IEnumerable<ProductRecord> products)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(products, s_options);

        File.WriteAllText(path, json + Environment.NewLine);
    }
}
