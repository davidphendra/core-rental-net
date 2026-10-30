using CoreRentalNet.BuildingBlocks.Application.Embeddings;
using OpenAI.Embeddings;

namespace CoreRentalNet.BuildingBlocks.Infrastructure.Embeddings;

/// <summary>
/// The protocol adapter both roots embed through, whichever provider built the underlying embedding client.
/// </summary>
/// <remarks>
/// <para>
/// <b>The width is requested, not assumed.</b> The model answers at 1536 dimensions by default, and the
/// vectors this solution stores are 384 wide, so every call asks for 384 through the OpenAI <c>dimensions</c>
/// option. Azure honours it for the <c>text-embedding-3</c> family; a local server may ignore it when the
/// model already has that width, so the answer is still checked by the caller.
/// </para>
/// <para>
/// <b>It builds no transport of its own.</b> The <see cref="EmbeddingClient"/> is handed to it by an
/// <c>IEmbeddingServiceFactory</c>. A class that constructs its own client cannot be exercised without a
/// live deployment; this one can be driven through a stand-in transport.
/// </para>
/// <para>
/// <b>It lives in the shared kernel because both composition roots embed with it:</b> the ingestion tool embeds
/// a catalogue's chunks, and the application embeds every customer's sentence. The two vectors are compared
/// with each other at query time, so they have to be produced at the same width — which is an argument for
/// one protocol adapter rather than one per provider.
/// </para>
/// </remarks>
public sealed class EmbeddingClientService : IEmbeddingService
{
    private readonly EmbeddingClient _embeddingClient;
    private readonly int _width;

    /// <summary>The adapter over one embedding client, asking for one width.</summary>
    /// <param name="embeddingClient">The client to send through, built by a factory or by a test.</param>
    /// <param name="width">The number of dimensions to request and to store.</param>
    public EmbeddingClientService(EmbeddingClient embeddingClient, int width)
    {
        ArgumentNullException.ThrowIfNull(embeddingClient);
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);

        _embeddingClient = embeddingClient;
        _width = width;
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
