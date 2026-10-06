using Azure;
using Azure.AI.OpenAI;
using CoreRentalNet.BuildingBlocks.Application.Embeddings;
using OpenAI.Embeddings;

namespace CoreRentalNet.BuildingBlocks.Infrastructure.Embeddings;

/// <summary>
/// The embedding client both roots embed through: the Azure OpenAI deployment that serves the model.
/// </summary>
/// <remarks>
/// <para>
/// <b>The width is requested, not assumed.</b> The model answers at 1536 dimensions by default, and the
/// vectors this solution stores are 384 wide, so every call asks for 384 through the OpenAI <c>dimensions</c>
/// option. Azure honours it for the <c>text-embedding-3</c> family; the width the answer carries is still
/// checked by the caller, because a number that was requested is not a number that was delivered.
/// </para>
/// <para>
/// <b>It builds no transport of its own.</b> The <see cref="EmbeddingClient"/> is handed to it, and
/// <see cref="Build"/> is a composition root's one line for constructing one. A class that constructs its own
/// client cannot be exercised without a live deployment; this one can be driven through a stand-in transport.
/// </para>
/// <para>
/// <b>It lives in the shared kernel because both composition roots embed with it:</b> the ingestion tool embeds
/// a catalogue's chunks, and the application embeds every customer's sentence. The two vectors are compared
/// with each other at query time, so they have to come from the same deployment at the same width — which is
/// an argument for one adapter rather than two that agree until one of them is edited.
/// </para>
/// </remarks>
public sealed class OpenAiCompatibleEmbeddingService : IEmbeddingService
{
    private readonly EmbeddingClient _embeddingClient;
    private readonly int _width;

    /// <summary>The adapter over one embedding client, asking for one width.</summary>
    /// <param name="embeddingClient">The client to send through, built by <see cref="Build"/> or by a test.</param>
    /// <param name="width">The number of dimensions to request and to store.</param>
    public OpenAiCompatibleEmbeddingService(EmbeddingClient embeddingClient, int width)
    {
        ArgumentNullException.ThrowIfNull(embeddingClient);
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);

        _embeddingClient = embeddingClient;
        _width = width;
    }

    /// <summary>The client for one Azure OpenAI deployment, reached with an API key.</summary>
    /// <param name="endpoint">The resource endpoint, e.g. https://contoso.openai.azure.com/.</param>
    /// <param name="apiKey">The deployment's key, from configuration or the environment and never a committed file.</param>
    /// <param name="deployment">The name the resource serves the model under.</param>
    /// <param name="width">The number of dimensions to request: 384, though the model's native width is 1536.</param>
    public static IEmbeddingService Build(Uri endpoint, string apiKey, string deployment, int width)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(deployment);
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);

        var client = new AzureOpenAIClient(endpoint, new AzureKeyCredential(apiKey));

        return new OpenAiCompatibleEmbeddingService(client.GetEmbeddingClient(deployment), width);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<float[]>> EmbedAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(texts);

        // One call for whatever the caller sent: the deployment takes many inputs per call, and no caller here
        // sends more than a batch of sentences, so there is no batching to do and no partial answer. The width
        // is stated here because the model would otherwise answer at 1536.
        var options = new EmbeddingGenerationOptions { Dimensions = _width };

        var answerEmbeddings = await _embeddingClient
            .GenerateEmbeddingsAsync(texts, options, cancellationToken)
            .ConfigureAwait(false);

        return [.. answerEmbeddings.Value.Select(embedding => embedding.ToFloats().ToArray())];
    }
}
