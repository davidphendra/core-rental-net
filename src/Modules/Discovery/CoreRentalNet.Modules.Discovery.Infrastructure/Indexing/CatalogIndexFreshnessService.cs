using CoreRentalNet.BuildingBlocks.Application.Embeddings;
using CoreRentalNet.BuildingBlocks.Infrastructure.Sqlite;
using CoreRentalNet.BuildingBlocks.Infrastructure.Vectors;
using CoreRentalNet.Modules.Discovery.Application.Indexing;
using Microsoft.Data.Sqlite;

namespace CoreRentalNet.Modules.Discovery.Infrastructure.Indexing;

/// <summary>Compares the recipe the ingestion tool wrote with what this deployment would build.</summary>
/// <remarks>
/// <para>
/// <b>The recipe is the tool's own record, read from the tool's own file.</b> It used to be a row this module
/// wrote beside its vectors; the vectors moved to the tool and the record moved with them, because a recipe is
/// only worth anything beside the rows it describes — kept apart, the two could disagree.
/// </para>
/// <para>
/// <b>The composition is compared against the shared contract, not against a renderer</b>, and that is a
/// consequence of the move: this module no longer renders a product's text at all, so it has nothing of its
/// own to compare with. A change to the tool's renderer must bump that contract, and this check is what makes
/// forgetting to loud rather than silent.
/// </para>
/// <para>
/// <b>The first disagreement is the one reported.</b> Several at once is possible and the operator only needs
/// one to start with; naming all four would be four ways of saying the vectors are stale, and the first is
/// usually the cause of the rest — a catalogue that moved is a hash change and may be nothing else.
/// </para>
/// </remarks>
public sealed class CatalogIndexFreshnessService : ICatalogIndexFreshnessService
{
    private readonly string _databasePath;
    private readonly SqliteDatabaseSettings _settings;

    /// <summary>The freshness check over one ingestion tool's vector file.</summary>
    public CatalogIndexFreshnessService(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        _databasePath = databasePath;
        _settings = SqliteDatabaseSettings.Local(databasePath);
    }

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

    /// <summary>The recipe the tool wrote, or null when there is no file, no table or no row.</summary>
    private EmbeddingRecipe? Read()
    {
        // A file that is not there is the same answer as an empty table, and opening one would create it —
        // leaving behind an empty database that looks built.
        if (!File.Exists(_databasePath))
        {
            return null;
        }

        using var connection = SqliteDatabase.Open(_settings);

        // A file written by a tool older than the recipe table answers "not built" rather than raising: the
        // operator's next action is the same either way, and it is to run the tool.
        if (!HasEmbeddingRecipeTable(connection))
        {
            return null;
        }

        using var command = connection.CreateCommand();
        command.CommandText =
            $"SELECT modelId, width, composition, catalogueHash FROM {ProductVectorContract.RecipeTable} LIMIT 1;";

        using var reader = command.ExecuteReader();

        return reader.Read()
            ? new EmbeddingRecipe(reader.GetString(0), reader.GetInt32(1), reader.GetString(2), reader.GetString(3))
            : null;
    }

    /// <summary>Whether the tool's file holds the recipe table at all.</summary>
    private static bool HasEmbeddingRecipeTable(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name = $name;";
        command.Parameters.AddWithValue("$name", ProductVectorContract.RecipeTable);

        return command.ExecuteScalar() is not null;
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
