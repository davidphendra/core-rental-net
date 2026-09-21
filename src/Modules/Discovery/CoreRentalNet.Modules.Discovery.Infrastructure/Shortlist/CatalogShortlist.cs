using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Discovery.Application.Embeddings;
using CoreRentalNet.Modules.Discovery.Application.Shortlist;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace CoreRentalNet.Modules.Discovery.Infrastructure.Shortlist;

/// <summary>
/// The shortlist: embed the request, read the vectors, and rank them per bucket.
/// </summary>
/// <remarks>
/// <para>
/// <b>This class holds no rule.</b> The embedding is a call and the ranking is <see cref="CatalogRanking"/>; what
/// is left here is the reading and the mapping, which is what an adapter is for. Keeping it that thin is also
/// what lets every rule the shortlist has be proved in a unit test rather than through a database.
/// </para>
/// <para>
/// <b>A SKU the catalogue does not hold is an error and not a product to skip.</b> It means the index describes
/// a catalogue that is no longer there — a state the freshness check refuses at start-up — and quietly dropping
/// the row would answer from a shortlist nothing reported as incomplete.
/// </para>
/// </remarks>
public sealed class CatalogShortlist(
    IEmbeddingClient embeddings,
    IProductCatalog catalogue,
    DiscoveryContext context,
    ShortlistSettings settings) : ICatalogShortlist
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ShortlistItem>> ForAsync(string query, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        var queryVector = await QueryVectorAsync(query, cancellationToken).ConfigureAwait(false);
        var candidates = await CandidatesAsync(cancellationToken).ConfigureAwait(false);

        return CatalogRanking.TopPerBucket(queryVector, candidates, settings.PerBucket, await BoostAsync(cancellationToken).ConfigureAwait(false));
    }

    /// <summary>The request itself, as one vector. Exactly one text in, exactly one vector out.</summary>
    /// <remarks>
    /// Both ways this can fail are the same fact — this deployment cannot embed a request right now — so both
    /// arrive as <see cref="ShortlistUnavailableException"/>. A deployment that refuses the call and a deployment
    /// that answers with three vectors for one text are equally unusable, and neither is a defect here.
    /// <b>Cancellation is not caught</b>: a customer who stopped the run has not met an outage, and the run has
    /// a separate, neutral ending for that.
    /// </remarks>
    private async Task<float[]> QueryVectorAsync(string query, CancellationToken cancellationToken)
    {
        IReadOnlyList<float[]> answer;

        try
        {
            answer = await embeddings.EmbedAsync([query], cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new ShortlistUnavailableException(
                $"The embedding deployment did not answer, so no shortlist could be built: {exception.Message}",
                exception);
        }

        return answer.Count == 1
            ? answer[0]
            : throw new ShortlistUnavailableException(
                $"Embedding one request returned {answer.Count} vectors, so there is no query vector to rank with.");
    }

    /// <summary>What customers have taken, or null when the signal is switched off.</summary>
    /// <remarks>
    /// <b>Read only when there is a weight to apply it with.</b> A deployment that has turned the signal off pays
    /// nothing for it — no second read, no dictionary — and the shortlist is then purely similarity, which is the
    /// behaviour the epic shipped before this story.
    /// </remarks>
    private async Task<SelectionBoost?> BoostAsync(CancellationToken cancellationToken)
    {
        if (settings.BoostWeight <= 0)
        {
            return null;
        }

        var ratios = await context.Selections
            .AsNoTracking()
            .Where(selection => selection.DecayedOffered > 0)
            .Select(selection => new { selection.Sku, selection.DecayedChosen, selection.DecayedOffered })
            .ToDictionaryAsync(
                row => row.Sku,
                row => row.DecayedChosen / row.DecayedOffered,
                cancellationToken)
            .ConfigureAwait(false);

        return new SelectionBoost(settings.BoostWeight, ratios);
    }

    /// <summary>Every stored vector, with the bucket its product belongs to.</summary>
    private async Task<IReadOnlyList<CatalogCandidate>> CandidatesAsync(CancellationToken cancellationToken)
    {
        var vectors = await context.Vectors.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);

        return [.. vectors.Select(vector => new CatalogCandidate(vector.Sku, BucketOf(vector.Sku), vector.Embedding))];
    }

    /// <summary>Which bucket a product belongs to, taken from the catalogue rather than stored beside the vector.</summary>
    /// <remarks>
    /// Reading it here is why the vector table has no bucket column: the catalogue already guarantees the
    /// mapping is total and the application already holds the catalogue, so a stored copy would be a second
    /// answer that can go out of step with the first.
    /// </remarks>
    private CatalogBucket BucketOf(string sku)
    {
        var product = catalogue.Find(sku)
            ?? throw new InvalidOperationException(
                $"The index holds '{sku}', which the catalogue does not. The index describes a catalogue that has changed; re-run CoreRentalNet.CatalogIngestion.");

        return new CatalogBucket(product.Category, product.SubCategory);
    }
}
