namespace CoreRentalNet.CatalogIngestion.Storage;

/// <summary>The vector table, as this tool needs it.</summary>
/// <remarks>
/// <para>
/// <b>Replace, never append.</b> A run writes the whole catalogue, so the table is emptied and refilled
/// together: re-running is safe, and no reader ever observes it half-written. A partial update would leave
/// rows for products the catalogue no longer has, and nothing would report them.
/// </para>
/// <para>
/// <b>Synchronous, because <c>Microsoft.Data.Sqlite</c> is.</b> Wrapping a synchronous call in a task would
/// add a thread hop and change nothing an operator could observe.
/// </para>
/// </remarks>
internal interface IProductEmbeddingStore
{
    /// <summary>Replaces every row with <paramref name="rows"/>, stamped with <paramref name="createdAt"/>.</summary>
    void ReplaceAll(IReadOnlyList<ProductEmbeddingRow> rows, DateTimeOffset createdAt);
}
