using CoreRentalNet.Modules.Discovery.Application;
using CoreRentalNet.Modules.Discovery.Application.Embeddings;
using CoreRentalNet.Modules.Discovery.Application.Ingestion;
using Microsoft.EntityFrameworkCore;

namespace CoreRentalNet.Modules.Discovery.Infrastructure.Ingestion;

/// <summary>
/// The ingestion: the catalogue's whole text is embedded in one call and written with the recipe beside it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Nothing is written until every vector is in hand.</b> The embeddings are requested first and checked
/// before the first write, so a deployment that answers with fewer vectors than texts, or with a width other
/// than the one declared, leaves the existing index exactly as it was. The alternative — writing as the
/// answers arrive — would replace a good index with a partial one and take the query path down with it.
/// </para>
/// <para>
/// <b>The replacement is one transaction.</b> Re-running the tool is therefore idempotent, and a reader never
/// observes an index that is half one catalogue and half another.
/// </para>
/// </remarks>
public sealed class CatalogIngestion(IEmbeddingClient embeddings, DiscoveryContext context) : ICatalogIngestion
{
    /// <summary>The index's name. One index today, and the name is recorded rather than assumed.</summary>
    public const string IndexName = "catalog";

    /// <inheritdoc />
    public async Task<int> IngestAsync(CatalogIngestionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var texts = request.Catalogue.Select(EmbeddedText.Of).ToArray();
        var vectors = await embeddings.EmbedAsync(texts, cancellationToken).ConfigureAwait(false);

        Check(texts, vectors, request.Width);

        await ReplaceAsync(request, vectors, cancellationToken).ConfigureAwait(false);

        return vectors.Count;
    }

    /// <summary>What makes an answer unusable. Checked before anything is written, and never repaired.</summary>
    /// <remarks>
    /// The catalogue is refused when empty for the same reason the loader refuses an empty file: an index of
    /// nothing would answer every query with no products and look like a working deployment.
    /// </remarks>
    private static void Check(string[] texts, IReadOnlyList<float[]> vectors, int width)
    {
        if (texts.Length == 0)
        {
            throw new InvalidOperationException("The catalogue held no products, so there is nothing to index.");
        }

        if (vectors.Count != texts.Length)
        {
            throw new InvalidOperationException(
                $"The embedding deployment returned {vectors.Count} vectors for {texts.Length} products. "
                + "A partial index is refused, and nothing has been written.");
        }

        foreach (var vector in vectors)
        {
            if (vector.Length != width)
            {
                throw new InvalidOperationException(
                    $"The embedding deployment returned a vector {vector.Length} wide where {width} was declared. "
                    + "A width is what makes a stored vector comparable with a query, so nothing has been written.");
            }
        }
    }

    /// <summary>Every row replaced in one transaction, with the recipe that describes them.</summary>
    private async Task ReplaceAsync(
        CatalogIngestionRequest request,
        IReadOnlyList<float[]> vectors,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await context.Vectors.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await context.Indexes.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);

        // REQUIRED, AND LEARNED BY RUNNING IT RATHER THAN BY READING IT. ExecuteDelete goes straight to the
        // database and leaves the change tracker holding entities that no longer exist, so a second ingestion
        // through the same context fails on `Add` with "another instance with the same key value is already
        // being tracked" - the keys overlap because the catalogue does. Clearing is correct rather than
        // convenient: this service replaces the whole table, so nothing the tracker holds is worth keeping,
        // and a caller must not use one context for an ingestion and other work at the same time.
        context.ChangeTracker.Clear();

        for (var position = 0; position < vectors.Count; position++)
        {
            context.Vectors.Add(new CatalogVector
            {
                Sku = request.Catalogue[position].Sku,
                Embedding = vectors[position],
            });
        }

        context.Indexes.Add(new CatalogIndex
        {
            Name = IndexName,
            ModelId = request.ModelId,
            Width = request.Width,
            // Read from the renderer rather than passed in, because a caller free to name the composition
            // could name one the text did not come from - and the recipe is the only thing that says so.
            Composition = EmbeddedText.Composition,
            CatalogueHash = request.CatalogueHash,
        });

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }
}
