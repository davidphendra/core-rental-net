using System.Text.Json;
using CoreRentalNet.Modules.Catalog.Domain;

namespace CoreRentalNet.Modules.Catalog.Infrastructure.Loading;

/// <summary>
/// Reads products.json into an immutable catalog. Fails loudly and specifically: every message names
/// the file and, where relevant, the offending SKU and field.
/// </summary>
/// <remarks>
/// Reading, mapping and checking for duplicates, in that order. What a record has to say is the
/// mapper's rule, and the image policy belongs to the images the mapper is given; what is left here is
/// the file itself.
/// </remarks>
public static class CatalogLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    public static ProductCatalogSnapshot LoadFromFile(string path, string? webRootPath = null)
    {
        var records = Read(path);
        var mapper = new CatalogRecordMapper(new ProductImages(webRootPath));

        var products = new List<Product>(records.Count);

        foreach (var record in records)
        {
            products.Add(mapper.Map(record, path));
        }

        EnsureNoDuplicateSkus(products, path);

        return new ProductCatalogSnapshot(products);
    }

    /// <summary>Reads the file and parses it, naming the file in every failure.</summary>
    private static List<CatalogFileRecord> Read(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new CatalogLoadException("No catalog file path was configured.");
        }

        if (!File.Exists(path))
        {
            throw new CatalogLoadException($"Catalog file not found: '{path}'.");
        }

        string json;

        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new CatalogLoadException($"Catalog file could not be read: '{path}'.", exception);
        }

        List<CatalogFileRecord>? records;

        try
        {
            records = JsonSerializer.Deserialize<List<CatalogFileRecord>>(json, Options);
        }
        catch (JsonException exception)
        {
            throw new CatalogLoadException(
                $"Catalog file is not valid JSON: '{path}'. {exception.Message}",
                exception);
        }

        if (records is null || records.Count == 0)
        {
            throw new CatalogLoadException($"Catalog file contains no products: '{path}'.");
        }

        return records;
    }

    private static void EnsureNoDuplicateSkus(IReadOnlyList<Product> products, string path)
    {
        var duplicates = products
            .GroupBy(product => product.Sku.Value, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(sku => sku, StringComparer.Ordinal)
            .ToArray();

        if (duplicates.Length > 0)
        {
            throw new CatalogLoadException(
                $"Catalog file '{path}' contains duplicate SKUs: {string.Join(", ", duplicates)}.");
        }
    }
}
