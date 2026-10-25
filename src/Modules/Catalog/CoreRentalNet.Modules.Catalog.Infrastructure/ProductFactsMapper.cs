using System.Text.Json;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Domain;
using CoreRentalNet.Modules.Catalog.Infrastructure.Persistence;

namespace CoreRentalNet.Modules.Catalog.Infrastructure;

/// <summary>Projects catalogue metadata into the compact facts the MCP read model publishes.</summary>
internal static class ProductFactsMapper
{
    private static readonly string[] Keys =
    [
        "brand", "color", "material", "dimensions", "type", "desktype",
        "style", "pattern", "lampType", "packQuantity", "packType",
        "screenCount", "setType", "subtype", "shape",
    ];

    public static ProductFacts? From(ProductMetadata metadata)
        => From(metadata.Attributes);

    public static ProductFacts? From(CatalogProduct row)
    {
        try
        {
            var attributes = JsonSerializer.Deserialize<Dictionary<string, string>>(row.AttributesJson)
                ?? [];
            return From(attributes);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static ProductFacts? From(IReadOnlyDictionary<string, string> attributes)
    {
        var values = attributes
            .Where(pair => Keys.Contains(pair.Key, StringComparer.OrdinalIgnoreCase)
                           && !string.IsNullOrWhiteSpace(pair.Value)
                           && !string.Equals(pair.Value, "unspecified", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

        return values.Count == 0 ? null : new ProductFacts(values);
    }
}
