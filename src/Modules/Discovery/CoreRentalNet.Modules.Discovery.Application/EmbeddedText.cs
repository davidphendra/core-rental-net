using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Modules.Discovery.Application;

/// <summary>
/// The text a product is embedded from, and the name of the composition that produced it.
/// </summary>
/// <remarks>
/// <para>
/// The catalogue publishes <see cref="ProductView"/>, and this turns one into the single string that is sent
/// to the embedding deployment. It is deliberately <b>the whole of what a product says about itself</b> —
/// name, description, tags, best-for, not-for and every attribute, prices and counts included — rather than a
/// curated selection. The alternative was considered and refused: a curated list is a second definition of a
/// product's text that can drift from the first, and the fields it would have dropped (the ratings, the
/// prices) are fields the catalogue already publishes and the agent is already shown.
/// </para>
/// <para>
/// <b>Nothing here may depend on the machine.</b> The lines are joined with a bare newline rather than
/// <c>Environment.NewLine</c>, and the attributes are ordered by key, so the same catalogue produces the same
/// bytes on every machine. If it did not, two ingestion runs over one unchanged file would produce different
/// vectors, and the hash recorded beside them would be describing something that cannot be reproduced.
/// </para>
/// </remarks>
public static class EmbeddedText
{
    /// <summary>
    /// What the composition is, recorded in the index beside the vectors so a change to this file is a
    /// detectable fact rather than a silent invalidation of every row.
    /// </summary>
    /// <remarks>
    /// The trailing <c>/1</c> is the composition's own version. It changes when the field list or the
    /// rendering changes, and it is the value <c>CatalogIndex.Composition</c> carries.
    /// </remarks>
    public const string Composition = "name+description+metadata/1";

    /// <summary>The text one product is embedded from.</summary>
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
        // dictionary the loader builds does not promise one, and an index has to be reproducible.
        foreach (var attribute in product.Metadata.Attributes.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            lines.Add($"attribute {attribute.Key}: {attribute.Value}");
        }

        return string.Join('\n', lines);
    }

    /// <summary>A list-valued part of the text, left out entirely when the product says nothing for it.</summary>
    /// <remarks>
    /// An empty section is omitted rather than written as an empty label, because a line that says nothing is
    /// a token the model would read as meaning something. Only 29 of the 205 products carry a <c>notFor</c>,
    /// so the alternative would have added a meaningless line to the other 176.
    /// </remarks>
    private static void Section(List<string> lines, string label, IReadOnlyList<string> values)
    {
        if (values.Count > 0)
        {
            lines.Add($"{label}: {string.Join(", ", values)}");
        }
    }
}
