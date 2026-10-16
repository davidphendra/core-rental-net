namespace CoreRentalNet.Host.Configs;

/// <summary>Where the embedding deployment is, what model it serves, and at what width it embeds.</summary>
/// <remarks>
/// <para>
/// <b>Every value is required together or the feature is off.</b> There is no default endpoint and no
/// default key: a deployment that has not been told which Azure OpenAI resource embeds its requests has
/// not been finished, and the honest failure is an unavailable search rather than an answer built from
/// vectors nothing can explain.
/// </para>
/// <para>
/// <b>Ingestion and retrieval embed through the same deployment and the same width.</b> The two vectors
/// are compared with each other, so the application asks the deployment the tool wrote the vectors with.
/// The tool records the model and the width it used, and this deployment's model and width are compared
/// against that record before anything is searched.
/// </para>
/// <para>
/// <b>The store is no longer a setting.</b> The vectors live in this application's own Discovery
/// database and are carried by its migrations, so a deployment no longer has to be told where the tool
/// left its file.
/// </para>
/// </remarks>
internal sealed record VectorEmbeddingSettings(
    string Endpoint,
    string ApiKey,
    string Model,
    int Width)
{
    /// <summary>The width requested from the model, and so the width the vectors are stored at.</summary>
    public const int DefaultWidth = 384;

    /// <summary>True when this deployment has been told where to embed, and with what.</summary>
    public bool IsConfigured
        => Uri.TryCreate(Endpoint, UriKind.Absolute, out _)
            && !string.IsNullOrWhiteSpace(ApiKey)
            && !string.IsNullOrWhiteSpace(Model)
            && Width > 0;

    public static VectorEmbeddingSettings From(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new VectorEmbeddingSettings(
            configuration["VectorEmbedding:Endpoint"]?.Trim() ?? string.Empty,
            configuration["VectorEmbedding:ApiKey"]?.Trim() ?? string.Empty,
            configuration["VectorEmbedding:Model"]?.Trim() ?? string.Empty,
            configuration.GetValue("VectorEmbedding:Width", DefaultWidth));
    }
}
