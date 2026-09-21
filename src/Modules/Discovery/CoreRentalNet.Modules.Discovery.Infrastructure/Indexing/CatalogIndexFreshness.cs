using CoreRentalNet.Modules.Discovery.Application;
using CoreRentalNet.Modules.Discovery.Application.Indexing;
using CoreRentalNet.Modules.Discovery.Infrastructure.Ingestion;
using Microsoft.EntityFrameworkCore;

namespace CoreRentalNet.Modules.Discovery.Infrastructure.Indexing;

/// <summary>
/// Compares the stored recipe with what this deployment would build.
/// </summary>
/// <remarks>
/// <para>
/// <b>The composition is compared against the module rather than against a setting.</b> It is the one value
/// that is not configuration: <c>EmbeddedText.Composition</c> is what the code renders today, and a developer
/// who changes the fields a product is embedded from changes it in the same edit. Comparing it here means an
/// index built before that edit is refused, instead of answering from vectors nothing can now reproduce.
/// </para>
/// <para>
/// <b>The first disagreement is the one reported.</b> A run of several is possible and the operator only needs
/// one to start with; naming all four would be four ways of saying the index is stale, and the first is
/// usually the cause of the rest — a catalogue that moved is a hash change and may be nothing else.
/// </para>
/// </remarks>
public sealed class CatalogIndexFreshness(DiscoveryContext context) : ICatalogIndexFreshness
{
    /// <inheritdoc />
    public async Task<CatalogIndexVerdict> CheckAsync(
        string catalogueHash,
        string modelId,
        int width,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogueHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);

        var recipe = await context.Indexes
            .AsNoTracking()
            .SingleOrDefaultAsync(row => row.Name == CatalogIngestion.IndexName, cancellationToken)
            .ConfigureAwait(false);

        return recipe is null ? CatalogIndexVerdict.NotBuilt : Compare(recipe, catalogueHash, modelId, width);
    }

    private static CatalogIndexVerdict Compare(CatalogIndex recipe, string catalogueHash, string modelId, int width)
    {
        if (!string.Equals(recipe.CatalogueHash, catalogueHash, StringComparison.Ordinal))
        {
            return CatalogIndexVerdict.Stale(
                $"the catalogue has changed since the index was built: it was built from {recipe.CatalogueHash}, and the file is now {catalogueHash}");
        }

        if (!string.Equals(recipe.ModelId, modelId, StringComparison.Ordinal))
        {
            return CatalogIndexVerdict.Stale(
                $"the index was built by {recipe.ModelId}, and this deployment embeds with {modelId}; vectors from two models are not comparable");
        }

        if (recipe.Width != width)
        {
            return CatalogIndexVerdict.Stale(
                $"the index holds vectors {recipe.Width} wide, and this deployment embeds at {width}");
        }

        if (!string.Equals(recipe.Composition, EmbeddedText.Composition, StringComparison.Ordinal))
        {
            return CatalogIndexVerdict.Stale(
                $"the index was built from the composition '{recipe.Composition}', and this build renders '{EmbeddedText.Composition}'");
        }

        return CatalogIndexVerdict.Usable;
    }
}
