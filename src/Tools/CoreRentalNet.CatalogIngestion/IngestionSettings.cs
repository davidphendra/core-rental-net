using CoreRentalNet.CatalogIngestion.Chunking;

namespace CoreRentalNet.CatalogIngestion;

/// <summary>Everything one ingestion run was configured with, already resolved and validated.</summary>
/// <remarks>
/// <para>
/// <b>Two values have no default, because there is no local server to fall back to.</b> The Azure OpenAI
/// endpoint and the deployment's key are what a run cannot invent, so an absent one stops the run rather than
/// targeting something that is not there. Every other value has a default, and those defaults — the model, the
/// width it is asked for, and the chunking measured against it — are what the configuration file only has to
/// state when it differs.
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
    string EmbeddingEndpoint,
    string EmbeddingApiKey,
    string EmbeddingModel,
    int Width,
    ChunkerSettings Chunker)
{
    /// <summary>products.json, relative to the tool's project directory.</summary>
    public const string DefaultCataloguePath = "../../shared/data/products.json";

    /// <summary>The vector file, relative to the tool's project directory.</summary>
    public const string DefaultDatabasePath = "App_Data/product_embedding.db";

    /// <summary>The deployment name, which is the identifier the resource serves the model under.</summary>
    public const string DefaultEmbeddingModel = "text-embedding-3-small";

    /// <summary>How wide the vectors are asked to be, though the model's native width is 1536.</summary>
    public const int DefaultWidth = 384;
}
