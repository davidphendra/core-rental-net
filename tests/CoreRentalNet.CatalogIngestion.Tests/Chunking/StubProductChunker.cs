using CoreRentalNet.CatalogIngestion.Chunking;

namespace CoreRentalNet.CatalogIngestion.Tests.Chunking;

/// <summary>A hand-written splitter: the same interface, no chunking judgement.</summary>
/// <remarks>
/// It cuts by position and produces vectors that depend only on the position, so a test can hold the
/// pipeline's arithmetic — how many rows, of what width, carrying which product — against a known answer.
/// Anything about where a boundary really falls belongs to the semantic splitter's own test.
/// </remarks>
internal sealed class StubProductChunker(int width, int chunksPerProduct = 1, int? answerWith = null) : IProductChunker
{
    /// <summary>Every text this splitter was asked about, in order.</summary>
    public List<string> Texts { get; } = [];

    /// <inheritdoc />
    public Task<IReadOnlyList<float[]>> ChunkAsync(string text, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(text);

        Texts.Add(text);

        var count = answerWith ?? chunksPerProduct;

        return Task.FromResult<IReadOnlyList<float[]>>([.. Enumerable.Range(0, count).Select(Vector)]);
    }

    private float[] Vector(int position)
        => [.. Enumerable.Range(0, width).Select(index => (position * width) + index)];
}
