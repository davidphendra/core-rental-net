using System.Text.Json;
using CoreRentalNet.Modules.Catalog.Domain;
using CoreRentalNet.Modules.Catalog.Infrastructure.Contracts;

namespace CoreRentalNet.Modules.Catalog.Infrastructure.Loader;

/// <summary>Turns the metadata object of a row into the record the domain holds, or says what is wrong.</summary>
/// <remarks>
/// The failures are the ones a hand-edited file actually produces: a missing object, a key nobody
/// declared, an attribute that is not text, and a blank tag. Each names the SKU and the field, for
/// the same reason the rest of the loader does - the file is still in hand when it refuses.
/// </remarks>
internal static class ProductMetadataMapper
{
    public static ProductMetadata Map(ProductMetadataJsonRecord? record, string sku, string path)
    {
        if (record is null)
        {
            throw new ProductLoadException($"Catalog entry '{sku}' has no metadata: '{path}'.");
        }

        RejectUnknownKeys(record, sku, path);

        return new ProductMetadata(
            Tags(record.Tags, sku, path),
            Attributes(record.Attributes, sku, path),
            Clean(record.BestFor, "bestFor", sku, path),
            Clean(record.NotFor, "notFor", sku, path));
    }

    private static void RejectUnknownKeys(ProductMetadataJsonRecord record, string sku, string path)
    {
        if (record.Unknown is not { Count: > 0 })
        {
            return;
        }

        var names = string.Join(", ", record.Unknown.Keys.OrderBy(key => key, StringComparer.Ordinal));

        throw new ProductLoadException($"Catalog entry '{sku}' has an unknown metadata key '{names}': '{path}'.");
    }

    /// <summary>The tags, which are what a criterion is most often matched against, so at least one.</summary>
    private static IReadOnlyList<string> Tags(List<string>? values, string sku, string path)
    {
        var tags = Clean(values, "tag", sku, path);

        if (tags.Count == 0)
        {
            throw new ProductLoadException($"Catalog entry '{sku}' has metadata with no tags: '{path}'.");
        }

        return tags.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static IReadOnlyDictionary<string, string> Attributes(
        Dictionary<string, JsonElement>? values,
        string sku,
        string path)
    {
        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in values ?? [])
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ProductLoadException($"Catalog entry '{sku}' has metadata with a blank attribute name: '{path}'.");
            }

            if (value.ValueKind != JsonValueKind.String)
            {
                throw new ProductLoadException(
                    $"Catalog entry '{sku}' has metadata attribute '{key}' that is not text: '{path}'.");
            }

            attributes[key.Trim()] = value.GetString()!.Trim();
        }

        return attributes;
    }

    private static IReadOnlyList<string> Clean(List<string>? values, string field, string sku, string path)
    {
        var cleaned = new List<string>();

        foreach (var value in values ?? [])
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ProductLoadException($"Catalog entry '{sku}' has a blank {field}: '{path}'.");
            }

            cleaned.Add(value.Trim());
        }

        return cleaned;
    }
}
