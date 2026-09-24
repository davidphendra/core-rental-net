using CoreRentalNet.CatalogIngestion.Chunking;

namespace CoreRentalNet.CatalogIngestion;

/// <summary>Everything one ingestion run was configured with, already resolved and validated.</summary>
/// <remarks>
/// <para>
/// <b>Every value has a default, and the defaults are the values this tool is known to work with</b> — the
/// local embedding server, its model, that model's width, and the chunking measured against it. An absent key
/// therefore runs the tool as intended, and the configuration file only has to say what differs.
/// </para>
/// <para>
/// <b>A value that is present but unusable is not the same as an absent one</b> and is refused rather than
/// replaced; <see cref="ConfigurationReader"/> owns that distinction, because nothing else records what a run
/// actually used.
/// </para>
/// </remarks>
internal sealed record IngestionSettings(
    string CataloguePath,
    string DatabasePath,
    string EmbeddingServer,
    string EmbeddingModel,
    int Width,
    ChunkerSettings Chunker)
{
    /// <summary>products.json, relative to the tool's project directory.</summary>
    public const string DefaultCataloguePath = "../../shared/data/products.json";

    /// <summary>The vector file, relative to the tool's project directory.</summary>
    public const string DefaultDatabasePath = "App_Data/product_embedding.db";

    /// <summary>The local OpenAI-compatible embedding server.</summary>
    public const string DefaultEmbeddingServer = "http://localhost:8080/v1";

    /// <summary>The embedding model that server serves.</summary>
    public const string DefaultEmbeddingModel = "all-MiniLM-L6-v2-embedding";

    /// <summary>How wide that model's vectors are.</summary>
    public const int DefaultWidth = 384;
}
