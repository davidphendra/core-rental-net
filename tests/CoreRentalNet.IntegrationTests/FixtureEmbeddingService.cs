using CoreRentalNet.BuildingBlocks.Application.Embeddings;
using CoreRentalNet.BuildingBlocks.Infrastructure.Vectors;

namespace CoreRentalNet.IntegrationTests;

/// <summary>
/// Serves a fixture phrase's own vector, so the handler embeds without a server.
/// </summary>
/// <remarks>
/// <para>
/// This is not a mock of the embedding server: the vector it returns is the real model's output for that exact
/// sentence, produced when the fixture was generated. What a test exercises is therefore the true geometry —
/// a paraphrase and the products it is near — while the suite stays offline and deterministic.
/// </para>
/// <para>
/// <b>A phrase the fixture does not hold is an error rather than a zero vector.</b> A silent default would turn
/// a typo in a test into a search that ranks by nothing and still returns products, which is the failure this
/// whole fixture exists to avoid.
/// </para>
/// </remarks>
internal sealed class FixtureEmbeddingService(CatalogEmbeddingFixture fixture) : IEmbeddingService
{
    /// <summary>The texts this service was asked for, so a test can assert the sentence that reached it.</summary>
    public IReadOnlyList<string>? Asked { get; private set; }

    public Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(texts);

        Asked = texts;

        return Task.FromResult<IReadOnlyList<float[]>>([.. texts.Select(VectorFor)]);
    }

    private float[] VectorFor(string text)
    {
        var vector =
            fixture.Queries.FirstOrDefault(query => query.Phrase == text)?.Vector
            ?? fixture.Characterisation.FirstOrDefault(phrase => phrase.Phrase == text)?.Vector
            ?? throw new InvalidOperationException(
                $"The fixture holds no vector for '{text}'. Every phrase a test asks with must be in it; "
                + "regenerate the fixture with regenerate-catalog-embedding-fixture.py.");

        return VectorBlob.ToFloats(vector);
    }
}
