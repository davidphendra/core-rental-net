namespace CoreRentalNet.Modules.Discovery.Infrastructure;

/// <summary>
/// One catalogue product's embedding: the row the shortlist is ranked from.
/// </summary>
/// <remarks>
/// <para>
/// Infrastructure-only, for the reason <c>NumberSequenceEntry</c> records about itself: nothing in this
/// module's published vocabulary is this row. The application asks for a shortlist and receives SKUs and
/// scores; how a product's vector is stored is nobody else's business, and the domain has no rule to state
/// about an array of floats.
/// </para>
/// <para>
/// There is deliberately <b>no bucket column</b>. Which bucket a product belongs to is the catalogue's own
/// <c>(category, subCategory)</c>, which the application already holds in memory and which the catalogue
/// module already guarantees is total over the catalogue. Copying it here would be a second copy of a
/// vocabulary, and the second copy is what drifts.
/// </para>
/// </remarks>
public sealed class CatalogVector
{
    /// <summary>The product this vector is for. The catalogue's own key, so no second identifier exists.</summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>The embedding, stored as a blob rather than as JSON text: 512 floats are 2,048 bytes of
    /// binary and roughly three times that written out as digits.</summary>
    /// <remarks>
    /// Named <c>Embedding</c> rather than <c>Vector</c> or <c>Values</c> and the name is not cosmetic:
    /// <b><c>VALUES</c> is a SQL keyword</b>, so a column so named is a syntax error in any hand-written
    /// statement and needs quoting in every tool that touches the file. The integration test that writes a
    /// malformed row through raw SQL is what found it.
    /// </remarks>
    public float[] Embedding { get; set; } = [];
}
