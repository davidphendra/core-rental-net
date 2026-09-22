using CoreRentalNet.BuildingBlocks.Application.Embeddings;
using Microsoft.Extensions.AI;

namespace CoreRentalNet.CatalogIngestion.Embeddings;

/// <summary>The tool's embedding port, shaped the way the chunking library asks for it.</summary>
/// <remarks>
/// <para>
/// <b>It refuses a short answer.</b> The library pairs each text with the embedding at the same position, so a
/// server that answered with fewer vectors than texts would silently pair one sentence with another
/// sentence's vector — and the boundary it then drew, and the chunk it produced, would be built on the wrong
/// comparison.
/// </para>
/// <para>
/// This is the one place the tool mentions the third-party abstraction: the library's constructor demands it,
/// and nothing else should have to know.
/// </para>
/// </remarks>
internal sealed class ClientEmbeddingGenerator(IEmbeddingRepository embeddings) : IEmbeddingGenerator<string, Embedding<float>>
{
    /// <inheritdoc />
    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);

        var texts = values.ToArray();
        var vectors = await embeddings.EmbedAsync(texts, cancellationToken).ConfigureAwait(false);

        if (vectors.Count != texts.Length)
        {
            throw new InvalidOperationException(
                $"The embedding server returned {vectors.Count} vectors for {texts.Length} texts. "
                + "A short answer would pair a sentence with another sentence's vector.");
        }

        return [.. vectors.Select(vector => new Embedding<float>(vector))];
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
