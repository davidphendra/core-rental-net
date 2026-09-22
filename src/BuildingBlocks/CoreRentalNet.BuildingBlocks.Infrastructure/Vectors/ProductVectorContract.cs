namespace CoreRentalNet.BuildingBlocks.Infrastructure.Vectors;

/// <summary>
/// What the ingestion tool writes and the application reads, named once.
/// </summary>
/// <remarks>
/// <para>
/// <b>Two projects write the SQL for these tables and neither can see the other's source.</b> The names are
/// shared rather than repeated because a table renamed on one side only is an application that reports the
/// index missing while the file holds it — and the operator then re-runs an ingestion that changes nothing.
/// </para>
/// <para>
/// <b>The vector table is a <c>vec0</c> virtual table.</b> Its vector column is declared
/// <c>float[width] distance_metric=cosine</c>, which is what makes a <c>MATCH</c> query rank by cosine distance;
/// the width is the recipe's own and the freshness check refuses a query vector of another width.
/// </para>
/// <para>
/// <b><see cref="Composition"/> is the text contract, and it is here rather than beside the renderer for a
/// reason that is worth stating.</b> The tool renders a product's text and records this value; the application
/// compares what it recorded against this value, because the application no longer renders a product at all. A
/// change to the renderer must therefore bump this constant in the same edit. The renderer's own test is what
/// makes that obligation visible, and the freshness check is what makes forgetting it loud rather than silent.
/// </para>
/// </remarks>
public static class ProductVectorContract
{
    /// <summary>The table of product vectors: one row per chunk of a product's text.</summary>
    public const string VectorTable = "product_embedding";

    /// <summary>The one-row table that records what the vectors were built from.</summary>
    public const string RecipeTable = "product_embedding_recipe";

    /// <summary>Which fields a product's embedded text is made of, and the version of that rendering.</summary>
    public const string Composition = "name+description+metadata/1";
}
