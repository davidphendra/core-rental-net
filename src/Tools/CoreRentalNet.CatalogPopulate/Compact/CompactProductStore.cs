using System.Text.Json;

namespace CoreRentalNet.CatalogPopulate.Compact;

/// <summary>Reads and writes the compact catalogue, one JSON object per line.</summary>
internal static class CompactProductStore
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
    };

    /// <summary>Every product in the compact file, in file order.</summary>
    public static IEnumerable<CompactProduct> Read(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Compact input not found: '{path}'. Run with --extract first.", path);
        }

        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var product = JsonSerializer.Deserialize<CompactProduct>(line, s_options);
            if (product is not null)
            {
                yield return product;
            }
        }
    }

    /// <summary>Writes the products, one JSON object per line, creating the directory when needed.</summary>
    public static void Write(string path, IEnumerable<CompactProduct> products)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var writer = new StreamWriter(path);
        foreach (var product in products)
        {
            writer.WriteLine(JsonSerializer.Serialize(product, s_options));
        }
    }
}
