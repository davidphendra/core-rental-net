using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using CoreRentalNet.Modules.Catalog.Infrastructure.Contracts;

namespace CoreRentalNet.Modules.Catalog.Infrastructure.Loader;

/// <summary>Reads the catalog file into its row shape, or says why it cannot.</summary>
/// <remarks>
/// The failure is deliberately loud: a broken file stops the application at start-up rather than
/// producing a half-populated shop, so every message names the file. The rows this returns are
/// infrastructure-only and never leave this layer.
/// </remarks>
internal static class ProductFile
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    /// <summary>Every row in the file, or a failure naming what is wrong with it.</summary>
    public static IReadOnlyList<ProductJsonRecord> ReadRows(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ProductLoadException("No catalog file path was configured.");
        }

        if (!File.Exists(path))
        {
            throw new ProductLoadException($"Catalog file not found: '{path}'.");
        }

        try
        {
            return JsonSerializer.Deserialize<List<ProductJsonRecord>>(Text(path), Options) ?? [];
        }
        catch (JsonException exception)
        {
            throw new ProductLoadException(
                $"Catalog file is not valid JSON: '{path}'. {exception.Message}",
                exception);
        }
    }

    private static string Text(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch (IOException exception)
        {
            throw Unreadable(path, exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw Unreadable(path, exception);
        }
    }

    /// <summary>Both read failures mean the same thing to a caller: the file cannot be read.</summary>
    private static ProductLoadException Unreadable(string path, Exception cause)
        => new($"Catalog file could not be read: '{path}'.", cause);

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        };

        // Read-only so the shared options cannot be changed by a caller.
        options.MakeReadOnly();
        return options;
    }
}
