using System.Text.Json;

namespace CoreRentalNet.CatalogPopulate.Sources;

/// <summary>Streams a JSONL file one row at a time, so a multi-gigabyte file is never loaded whole.</summary>
internal static class JsonLineReader
{
    /// <summary>Every row the file parses, in file order. A row that does not parse is skipped.</summary>
    public static IEnumerable<T> Read<T>(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Source file not found: '{path}'.", path);
        }

        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            T? row;
            try
            {
                row = JsonSerializer.Deserialize<T>(line, SourceJson.Options);
            }
            catch (JsonException)
            {
                continue;
            }

            if (row is not null)
            {
                yield return row;
            }
        }
    }
}
