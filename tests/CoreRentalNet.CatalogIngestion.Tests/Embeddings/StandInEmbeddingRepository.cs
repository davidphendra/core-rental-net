using CoreRentalNet.BuildingBlocks.Application.Embeddings;

namespace CoreRentalNet.CatalogIngestion.Tests.Embeddings;

/// <summary>A hand-written embedding server: the same interface, no network, real vectors of the right width.</summary>
/// <remarks>
/// <para>
/// It is deliberately not a mock: it produces a genuine answer of the requested shape, so what a test
/// exercises is the tool's own logic rather than an expectation about the calls it makes.
/// </para>
/// <para>
/// <b>Its vectors mean nothing, and that is stated rather than hidden.</b> A vector is a function of the
/// text's characters, so two similar sentences are not near each other and no test using this can say
/// anything about retrieval quality. What it proves is the pipeline's shape and its refusals.
/// </para>
/// </remarks>
internal sealed class StandInEmbeddingRepository(int width, int? answerWith = null) : IEmbeddingRepository
{
    /// <summary>Every batch of texts this client was asked to embed, in order.</summary>
    public List<IReadOnlyList<string>> Calls { get; } = [];

    /// <inheritdoc />
    public Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(texts);

        Calls.Add(texts);

        // A count to answer with instead of the number asked for, so a test can hold a short answer against
        // the refusal that must follow.
        return answerWith is { } count
            ? Task.FromResult<IReadOnlyList<float[]>>([.. Enumerable.Range(0, count).Select(_ => new float[width])])
            : Task.FromResult<IReadOnlyList<float[]>>([.. texts.Select(For)]);
    }

    /// <summary>A vector that depends only on the text, so the same text always embeds the same way.</summary>
    private float[] For(string text)
    {
        var hash = text.Aggregate(17, (accumulator, character) => unchecked((accumulator * 31) + character));

        return [.. Enumerable.Range(0, width).Select(index => ((hash + (index * 7)) % 1000) / 1000f)];
    }
}
