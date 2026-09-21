using CoreRentalNet.CatalogIngestion.Chunking;
using CoreRentalNet.CatalogIngestion.Storage;
using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.CatalogIngestion;

/// <summary>The run: read the catalogue, chunk and embed every product, then write the table once.</summary>
/// <remarks>
/// <para>
/// <b>Nothing is written until every vector is in hand.</b> Every product is chunked, embedded and checked
/// before the first write, so a product with no chunks, or a vector of the wrong width, leaves the existing
/// table exactly as it was. Writing as the answers arrived would replace a good table with a partial one.
/// </para>
/// <para>
/// <b>The replacement is one transaction</b>, owned by the store, so a reader never observes a half-written
/// table and re-running the tool is idempotent.
/// </para>
/// <para>
/// <b>It depends on the catalogue's published port and on a width, not on a loader and a configuration.</b>
/// Reading the file is <see cref="IProductCatalog"/>'s business, and the only setting this run needs is the
/// number it checks each vector against.
/// </para>
/// </remarks>
internal sealed class IngestionPipeline(
    IProductCatalog catalogue,
    IProductChunker chunker,
    IProductEmbeddingStore store,
    int width,
    TimeProvider clock)
{
    /// <summary>Writes the catalogue's vectors, and answers how many rows were written.</summary>
    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var products = catalogue.All;

        // Defence in depth rather than a state that can be reached: the loader refuses a file holding no
        // products, so the catalogue cannot be empty today. It is kept because writing an empty table would
        // replace a good one with nothing, and a future catalogue source is not obliged to refuse it.
        if (products.Count == 0)
        {
            throw new InvalidOperationException("The catalogue held no products, so there is nothing to index.");
        }

        var rows = new List<ProductEmbeddingRow>();

        foreach (var product in products)
        {
            var vectors = await chunker
                .ChunkAsync(EmbeddedText.Of(product), cancellationToken)
                .ConfigureAwait(false);

            RefuseUnusable(product, vectors);

            rows.AddRange(vectors.Select(vector => new ProductEmbeddingRow(
                product.Sku,
                product.Name,
                product.Description,
                vector)));
        }

        store.ReplaceAll(rows, clock.GetUtcNow());

        return rows.Count;
    }

    /// <summary>What makes a product's vectors unusable. Checked before anything is written, never repaired.</summary>
    /// <remarks>
    /// A product with no chunks is refused rather than skipped: a table missing one product answers as though
    /// the catalogue never had it, which is a silent omission rather than a smaller result.
    /// </remarks>
    private void RefuseUnusable(ProductView product, IReadOnlyList<float[]> vectors)
    {
        if (vectors.Count == 0)
        {
            throw new InvalidOperationException(
                $"Product '{product.Sku}' produced no chunks, so it would be absent from the table. Nothing has been written.");
        }

        float[]? miswidthed = vectors.FirstOrDefault(vector => vector.Length != width);

        if (miswidthed is not null)
        {
            throw new InvalidOperationException(
                $"The embedding server returned a vector {miswidthed.Length} wide where {width} was configured. "
                + "A width is what makes a stored vector comparable with a query, so nothing has been written.");
        }
    }
}
