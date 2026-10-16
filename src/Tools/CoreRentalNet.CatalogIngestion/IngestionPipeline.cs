using CoreRentalNet.BuildingBlocks.Application.Embeddings;
using CoreRentalNet.CatalogIngestion.Chunking;
using CoreRentalNet.CatalogIngestion.Reporting;
using CoreRentalNet.CatalogIngestion.Storage;
using CoreRentalNet.Modules.Catalog.Domain;

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
/// <b>It depends on the catalogue records and on a recipe.</b>
/// Reading the file is <see cref="CatalogueFile"/>'s business, and what this run needs beyond that is what it
/// checks each vector against and what it records beside the vectors — which is the recipe, and nothing else
/// out of the settings file.
/// </para>
/// </remarks>
internal sealed class IngestionPipeline(
    IReadOnlyList<Product> catalogue,
    IProductChunker chunker,
    IProductEmbeddingStore store,
    IProgressReporter reporter,
    EmbeddingRecipe recipe,
    TimeProvider clock)
{
    /// <summary>Writes the catalogue's vectors, and answers how many rows were written.</summary>
    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var products = catalogue;

        // Defence in depth rather than a state that can be reached: the loader refuses a file holding no
        // products, so the catalogue cannot be empty today. It is kept because writing an empty table would
        // replace a good one with nothing, and a future catalogue source is not obliged to refuse it.
        if (products.Count == 0)
        {
            throw new InvalidOperationException("The catalogue held no products, so there is nothing to index.");
        }

        var rows = new List<ProductEmbeddingRow>();

        reporter.Report(new ProgressUpdate(
            "Chunking and embedding", 0, products.Count, $"{products.Count} products"));

        for (var index = 0; index < products.Count; index++)
        {
            var product = products[index];
            reporter.Report(new ProgressUpdate(
                "Chunking and embedding", index, products.Count, $"{product.Sku} · {product.Name}"));

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

        reporter.Report(new ProgressUpdate(
            "Chunking and embedding", products.Count, products.Count, $"{rows.Count} vectors"));

        reporter.Report(new ProgressUpdate("Writing vectors", 0, rows.Count));
        store.ReplaceAll(rows, recipe, clock.GetUtcNow());
        reporter.Report(new ProgressUpdate("Writing vectors", rows.Count, rows.Count, $"{rows.Count} rows"));

        return rows.Count;
    }

    /// <summary>What makes a product's vectors unusable. Checked before anything is written, never repaired.</summary>
    /// <remarks>
    /// A product with no chunks is refused rather than skipped: a table missing one product answers as though
    /// the catalogue never had it, which is a silent omission rather than a smaller result.
    /// </remarks>
    private void RefuseUnusable(Product product, IReadOnlyList<float[]> vectors)
    {
        if (vectors.Count == 0)
        {
            throw new InvalidOperationException(
                $"Product '{product.Sku}' produced no chunks, so it would be absent from the table. Nothing has been written.");
        }

        float[]? miswidthed = vectors.FirstOrDefault(vector => vector.Length != recipe.Width);

        if (miswidthed is not null)
        {
            throw new InvalidOperationException(
                $"The embedding server returned a vector {miswidthed.Length} wide where {recipe.Width} was configured. "
                + "A width is what makes a stored vector comparable with a query, so nothing has been written.");
        }
    }
}
