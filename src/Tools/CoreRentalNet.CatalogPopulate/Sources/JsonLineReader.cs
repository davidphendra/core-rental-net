using System.Text.Json;

namespace CoreRentalNet.CatalogPopulate.Sources;

/// <summary>Streams a JSONL file one row at a time, so a multi-gigabyte file is never loaded whole.</summary>
internal static class JsonLineReader
{
    /// <summary>Every row the file parses, in file order. A row that does not parse is skipped.</summary>
    /// <remarks>
    /// <paramref name="onBytesRead"/> is given the bytes consumed so far each time a line is read, so a caller
    /// scanning a multi-gigabyte file can show how far it has got rather than only how many rows it accepted.
    /// The figure is the stream's read-ahead position, so it is exact enough for a progress bar and not a
    /// claim about the last parsed row.
    /// </remarks>
    public static IEnumerable<T> Read<T>(string path, Action<long>? onBytesRead = null)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Source file not found: '{path}'.", path);
        }

        using var stream = File.OpenRead(path);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            onBytesRead?.Invoke(stream.Position);

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
