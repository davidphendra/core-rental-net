using CoreRentalNet.BuildingBlocks.Application.Embeddings;
using CoreRentalNet.CatalogIngestion.Chunking;

namespace CoreRentalNet.CatalogIngestion;

/// <summary>Everything one ingestion run was configured with, already resolved and validated.</summary>
/// <remarks>
/// <para>
/// <b>The embedding choices are one value, not five fields.</b> The same <see cref="EmbeddingSettings"/> the
/// application uses is handed to the shared factory here, so the tool and the application cannot describe
/// the same embedding server differently.
/// </para>
/// <para>
/// <b>A value that is present but unusable is not the same as an absent one</b> and is refused rather than
/// replaced; <see cref="ConfigurationReader"/> owns that distinction, because nothing else records what a run
/// actually used.
/// </para>
/// </remarks>
internal sealed record IngestionSettings(
    string CatalogueDatabasePath,
    string DatabasePath,
    EmbeddingSettings Embedding,
    ChunkerSettings Chunker)
{
    /// <summary>catalog.db, relative to the tool's project directory.</summary>
    public const string DefaultCatalogueDatabasePath = "../../Host/CoreRentalNet.Host/App_Data/catalog.db";

    /// <summary>The Discovery store the application reads, relative to the tool's project directory.</summary>
    /// <remarks>
    /// The vectors live in this module's own database now, so a run writes where the application reads rather
    /// than beside the tool for someone to copy across.
    /// </remarks>
    public const string DefaultDatabasePath = "../../Host/CoreRentalNet.Host/App_Data/discovery.db";

    /// <summary>The deployment name, which is the identifier the server serves the model under.</summary>
    public const string DefaultEmbeddingModel = "text-embedding-3-small";

    /// <summary>How wide the vectors are asked to be, though the model's native width can be larger.</summary>
    public const int DefaultWidth = 384;
}
