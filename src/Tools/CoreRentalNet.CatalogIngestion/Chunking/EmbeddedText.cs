using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.CatalogIngestion.Chunking;

/// <summary>The text a product is chunked from and embedded from.</summary>
/// <remarks>
/// <para>
/// It is deliberately <b>the whole of what a product says about itself</b> — name, description, tags,
/// best-for, not-for and every attribute, prices and counts included — rather than a curated selection. A
/// curated list would be a second definition of a product's text that can drift from the first, and the
/// fields it dropped are fields the catalogue already publishes.
/// </para>
/// <para>
/// <b>Nothing here may depend on the machine.</b> The lines are joined with a bare newline rather than
/// <c>Environment.NewLine</c>, and the attributes are ordered by key, so the same catalogue renders the same
/// bytes on every machine. If it did not, two runs over one unchanged file would produce different vectors
/// and nothing would say why.
/// </para>
/// </remarks>
internal static class EmbeddedText
{
    /// <summary>The text one product is chunked and embedded from.</summary>
    /// <param name="product">The catalogue's published view of a product.</param>
    public static string Of(ProductView product)
    {
        ArgumentNullException.ThrowIfNull(product);

        var lines = new List<string>
        {
            $"name: {product.Name}",
            $"description: {product.Description}",
        };

        Section(lines, "tags", product.Metadata.Tags);
        Section(lines, "bestFor", product.Metadata.BestFor);
        Section(lines, "notFor", product.Metadata.NotFor);

        // Ordered so the text does not depend on the order the attributes happened to arrive in: the
        // dictionary the loader builds does not promise one, and an embedded text has to be reproducible.
        foreach (var attribute in product.Metadata.Attributes.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            lines.Add($"attribute {attribute.Key}: {attribute.Value}");
        }

        return string.Join('\n', lines);
    }

    /// <summary>A list-valued part of the text, left out entirely when the product says nothing for it.</summary>
    /// <remarks>
    /// An empty section is omitted rather than written as an empty label, because a line that says nothing is
    /// a token the model would read as meaning something. Only a minority of the catalogue carries a
    /// <c>notFor</c>, so the alternative would add a meaningless line to most products.
    /// </remarks>
    private static void Section(List<string> lines, string label, IReadOnlyList<string> values)
    {
        if (values.Count > 0)
        {
            lines.Add($"{label}: {string.Join(", ", values)}");
        }
    }
}
