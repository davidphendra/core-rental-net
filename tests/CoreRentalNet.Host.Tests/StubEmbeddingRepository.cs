using CoreRentalNet.BuildingBlocks.Application.Embeddings;

namespace CoreRentalNet.Host.Tests;

/// <summary>An embedding service a test answers with, so the endpoint can search without a server.</summary>
/// <remarks>
/// The handler embeds the sentence before it asks the similarity port, so the API tests need a vector to come
/// from somewhere. This returns one fixed vector; what it is does not matter to an endpoint test.
/// </remarks>
internal sealed class StubEmbeddingRepository : IEmbeddingRepository
{
    /// <inheritdoc />
    public Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<float[]>>([[0.1f, 0.2f, 0.3f]]);
}
