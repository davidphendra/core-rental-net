using CoreRentalNet.BuildingBlocks.Application.Embeddings;

namespace CoreRentalNet.CatalogIngestion.Storage;

/// <summary>The vector table and its recipe, as this tool needs them.</summary>
/// <remarks>
/// <para>
/// <b>Replace, never append.</b> A run writes the whole catalogue, so the tables are emptied and refilled
/// together: re-running is safe, and no reader ever observes one half written. A partial update would leave
/// rows for products the catalogue no longer has, and nothing would report them.
/// </para>
/// <para>
/// <b>The recipe is written as part of the same replacement, not beside it.</b> It is what tells a reader
/// whether the vectors can be searched at all, so a recipe describing a different run would be worse than no
/// recipe: it would be trusted. Both tables move together or neither does.
/// </para>
/// <para>
/// <b>Synchronous, because <c>Microsoft.Data.Sqlite</c> is.</b> Wrapping a synchronous call in a task would
/// add a thread hop and change nothing an operator could observe.
/// </para>
/// </remarks>
internal interface IProductEmbeddingStore
{
    /// <summary>
    /// Replaces every row with <paramref name="rows"/>, records <paramref name="recipe"/> as what built them,
    /// and stamps both with <paramref name="createdAt"/>.
    /// </summary>
    void ReplaceAll(IReadOnlyList<ProductEmbeddingRow> rows, EmbeddingRecipe recipe, DateTimeOffset createdAt);
}
