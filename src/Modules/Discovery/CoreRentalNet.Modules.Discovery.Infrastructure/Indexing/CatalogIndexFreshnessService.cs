using CoreRentalNet.BuildingBlocks.Application.Embeddings;
using CoreRentalNet.Modules.Discovery.Application.Indexing;
using CoreRentalNet.Modules.Discovery.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CoreRentalNet.Modules.Discovery.Infrastructure.Indexing;

/// <summary>Compares the recipe the store holds with what this deployment would build.</summary>
/// <remarks>
/// <para>
/// <b>The recipe is read through the context that owns the store.</b> It is an ordinary mapped table now, so
/// the read is a typed query rather than SQL that repeats its columns: a column renamed on the entity cannot
/// silently stop being read here.
/// </para>
/// <para>
/// <b>The composition is compared against the shared contract, not against a renderer.</b> This module does
/// not render a product's text, so it has nothing of its own to compare with. A change to the tool's renderer
/// must bump that contract, and this check is what makes forgetting it loud rather than silent.
/// </para>
/// <para>
/// <b>The first disagreement is the one reported.</b> Several at once is possible and the operator only needs
/// one to start with; naming all four would be four ways of saying the vectors are stale, and the first is
/// usually the cause of the rest — a catalogue that moved is a hash change and may be nothing else.
/// </para>
/// </remarks>
public sealed class CatalogIndexFreshnessService(EmbeddingContext context) : ICatalogIndexFreshnessService
{
    /// <inheritdoc />
    public CatalogIndexVerdict Check(string catalogueHash, string modelId, int width, string composition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogueHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);
        ArgumentException.ThrowIfNullOrWhiteSpace(composition);

        var recipe = Read();

        return recipe is null
            ? CatalogIndexVerdict.NotBuilt
            : Compare(recipe, catalogueHash, modelId, width, composition);
    }

    /// <summary>The recipe the store holds, or null when there is no file, no table or no row.</summary>
    private EmbeddingRecipe? Read()
    {
        // A file that is not there is the same answer as an empty table, and EF would create one on its first
        // query — leaving behind an empty database that looks built. The path comes from the context's own
        // connection, so the guard needs nothing new.
        if (!File.Exists(context.Database.GetDbConnection().DataSource))
        {
            return null;
        }

        ProductVectorRecipe? recipe;

        try
        {
            recipe = context.Recipe.FirstOrDefault();
        }
        catch (SqliteException)
        {
            // A store an older tool wrote has no recipe table. The operator's next action is the same either
            // way — run the ingestion tool — so a missing table answers "not built" rather than raising out of
            // a start-up check.
            return null;
        }

        return recipe is null
            ? null
            : new EmbeddingRecipe(recipe.ModelId, recipe.Width, recipe.Composition, recipe.CatalogueHash);
    }

    /// <summary>Compares what was recorded with what this deployment would build now.</summary>
    private static CatalogIndexVerdict Compare(
        EmbeddingRecipe recipe,
        string catalogueHash,
        string modelId,
        int width,
        string composition)
    {
        if (!string.Equals(recipe.CatalogueHash, catalogueHash, StringComparison.Ordinal))
        {
            return CatalogIndexVerdict.Stale(
                $"the catalogue has changed since the vectors were built: they were built from {recipe.CatalogueHash}, and the file is now {catalogueHash}");
        }

        if (!string.Equals(recipe.ModelId, modelId, StringComparison.Ordinal))
        {
            return CatalogIndexVerdict.Stale(
                $"the vectors were built by {recipe.ModelId}, and this deployment embeds with {modelId}; vectors from two models are not comparable");
        }

        if (recipe.Width != width)
        {
            return CatalogIndexVerdict.Stale(
                $"the stored vectors are {recipe.Width} wide, and this deployment embeds at {width}");
        }

        if (!string.Equals(recipe.Composition, composition, StringComparison.Ordinal))
        {
            return CatalogIndexVerdict.Stale(
                $"the vectors were built from the composition '{recipe.Composition}', and this build renders '{composition}'");
        }

        return CatalogIndexVerdict.Usable;
    }
}
