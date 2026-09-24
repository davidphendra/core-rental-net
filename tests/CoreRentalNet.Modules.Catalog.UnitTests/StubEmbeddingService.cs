using CoreRentalNet.BuildingBlocks.Application.Embeddings;

namespace CoreRentalNet.Modules.Catalog.UnitTests;

/// <summary>An embedding service a test decides the one vector of, so the handler can embed without a server.</summary>
internal sealed class StubEmbeddingService(float[] vector) : IEmbeddingService
{
    /// <summary>The texts the handler embedded, so a test can assert the sentence it carried.</summary>
    public IReadOnlyList<string>? Asked { get; private set; }

    public Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken)
    {
        Asked = texts;

        return Task.FromResult<IReadOnlyList<float[]>>([vector]);
    }
}
