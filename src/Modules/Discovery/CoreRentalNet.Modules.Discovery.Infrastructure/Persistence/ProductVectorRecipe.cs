namespace CoreRentalNet.Modules.Discovery.Infrastructure.Persistence;

/// <summary>
/// The one row that says what built the vectors beside it.
/// </summary>
/// <remarks>
/// <b>The shape is the ingestion file's, unchanged.</b> The columns are the ones
/// <c>product_embedding.db</c> already holds, so the migration reproduces that table rather than
/// redrawing it. The table has no primary key, which is why the entity is keyless: adding one would
/// be a structural change the file never had.
/// </remarks>
internal sealed class ProductVectorRecipe
{
    public string ModelId { get; set; } = string.Empty;

    public int Width { get; set; }

    public string Composition { get; set; } = string.Empty;

    public string CatalogueHash { get; set; } = string.Empty;

    public string CreatedAt { get; set; } = string.Empty;
}
