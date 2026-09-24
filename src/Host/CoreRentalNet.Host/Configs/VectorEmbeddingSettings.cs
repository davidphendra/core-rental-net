namespace CoreRentalNet.Host.Configs;

/// <summary>Where the embedding server is, what model it serves, and where the vectors it filled are.</summary>
/// <remarks>
/// <para>
/// <b>Every value is required together or the feature is off.</b> There is no default server, no default model
/// and no default file: a deployment that has not been told which server embeds its requests has not been
/// finished, and the honest failure is an unavailable search rather than an answer built from vectors nothing
/// can explain.
/// </para>
/// <para>
/// <b>It points at the ingestion tool's server rather than at a cloud deployment.</b> Ingestion and retrieval
/// have to embed through the same model or the two vectors cannot be compared at all, so the application asks
/// the same local OpenAI-compatible server the tool wrote the vectors with. The tool records the model it used
/// and this deployment's is compared against that record before anything is searched.
/// </para>
/// <para>
/// <b>The file is a setting because the tool owns it.</b> The tool writes its own SQLite file beside itself and
/// this application only ever reads it, so the path is the one fact the two must agree on — and it is agreed in
/// configuration rather than in code, because only the operator knows where the tool was run.
/// </para>
/// </remarks>
internal sealed record VectorEmbeddingSettings(
    string Server,
    string Model,
    string EmbeddingDatabase,
    int Width)
{
    /// <summary>The width the ingestion tool writes at, and so the width this deployment has to ask for.</summary>
    public const int DefaultWidth = 384;

    /// <summary>True when this deployment has been told where to embed and where the vectors are.</summary>
    public bool IsConfigured
        => !string.IsNullOrWhiteSpace(Server)
            && !string.IsNullOrWhiteSpace(Model)
            && !string.IsNullOrWhiteSpace(EmbeddingDatabase)
            && Width > 0;

    public static VectorEmbeddingSettings From(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new VectorEmbeddingSettings(
            configuration["VectorEmbedding:Server"]?.Trim() ?? string.Empty,
            configuration["VectorEmbedding:Model"]?.Trim() ?? string.Empty,
            configuration["VectorEmbedding:EmbeddingDatabase"]?.Trim() ?? string.Empty,
            configuration.GetValue("VectorEmbedding:Width", DefaultWidth));
    }
}
