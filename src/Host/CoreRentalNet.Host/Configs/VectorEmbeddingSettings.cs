using CoreRentalNet.BuildingBlocks.Application.Embeddings;

namespace CoreRentalNet.Host.Configs;

/// <summary>Where the embedding server is, what model it serves, and at what width it embeds.</summary>
/// <remarks>
/// <para>
/// <b>The provider is a setting because two are supported:</b> a hosted deployment uses Azure OpenAI, and a
/// developer's machine may use a local OpenAI-compatible server. The rest of the application only sees
/// <see cref="EmbeddingSettings"/>, so nothing downstream has to know which one this is.
/// </para>
/// <para>
/// <b>Ingestion and retrieval embed through the same server and the same width.</b> The two vectors are
/// compared with each other, so the application asks the server the tool wrote the vectors with. The tool
/// records the model and the width it used, and this deployment's model and width are compared against that
/// record before anything is searched.
/// </para>
/// <para>
/// <b>The store is no longer a setting.</b> The vectors live in this application's own Discovery database and
/// are carried by its migrations, so a deployment no longer has to be told where the tool left its file.
/// </para>
/// </remarks>
internal sealed record VectorEmbeddingSettings(
    EmbeddingProvider Provider,
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
            && (Provider == EmbeddingProvider.OpenAiCompatible
                || !string.IsNullOrWhiteSpace(ApiKey))
            && !string.IsNullOrWhiteSpace(Model)
            && Width > 0;

    /// <summary>The provider-neutral value the factory consumes.</summary>
    public EmbeddingSettings ToEmbeddingSettings()
        => new(
            Provider,
            new Uri(Endpoint, UriKind.Absolute),
            string.IsNullOrWhiteSpace(ApiKey) ? null : ApiKey,
            Model,
            Width);

    public static VectorEmbeddingSettings From(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new VectorEmbeddingSettings(
            ReadProvider(configuration),
            configuration["VectorEmbedding:Endpoint"]?.Trim() ?? string.Empty,
            configuration["VectorEmbedding:ApiKey"]?.Trim() ?? string.Empty,
            configuration["VectorEmbedding:Model"]?.Trim() ?? string.Empty,
            configuration.GetValue("VectorEmbedding:Width", DefaultWidth));
    }

    private static EmbeddingProvider ReadProvider(IConfiguration configuration)
    {
        var stated = configuration["VectorEmbedding:Provider"]?.Trim();

        if (string.IsNullOrEmpty(stated))
        {
            return EmbeddingProvider.AzureOpenAI;
        }

        return Enum.TryParse<EmbeddingProvider>(stated, ignoreCase: true, out var provider)
            ? provider
            : throw new InvalidOperationException(
                $"VectorEmbedding:Provider must be {nameof(EmbeddingProvider.AzureOpenAI)} or "
                + $"{nameof(EmbeddingProvider.OpenAiCompatible)}, but was '{stated}'.");
    }
}
