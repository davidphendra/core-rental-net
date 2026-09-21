using CoreRentalNet.Modules.Discovery.Application.Embeddings;

namespace CoreRentalNet.IntegrationTests;

/// <summary>
/// A hand-written stand-in for the embedding deployment: the same interface, no network, no credential.
/// </summary>
/// <remarks>
/// <para>
/// It is the reason the ingestion has an offline tier at all, and it is deliberately not a mock: it produces
/// a real answer of the right shape, so what a test exercises is the ingestion's own logic rather than an
/// expectation about the calls it makes.
/// </para>
/// <para>
/// <b>Its vectors mean nothing, and that is stated rather than hidden.</b> A vector here is a function of the
/// text's characters, so two similar sentences are not near each other and no test using this can say
/// anything about retrieval quality. That question belongs to the opt-in live tier, and a suite that quietly
/// implied otherwise would be worse than one that says so.
/// </para>
/// </remarks>
internal sealed class StandInEmbeddingClient : IEmbeddingClient
{
    private readonly int _width;
    private readonly int? _answerWith;

    /// <param name="width">The width every vector is produced at.</param>
    /// <param name="answerWith">A vector count to answer with instead of the number asked for, so a test can
    /// hold a short answer against the refusal that must follow.</param>
    public StandInEmbeddingClient(int width, int? answerWith = null)
    {
        _width = width;
        _answerWith = answerWith;
    }

    /// <summary>Every batch of texts this client was asked to embed, in order.</summary>
    public List<IReadOnlyList<string>> Calls { get; } = [];

    public Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(texts);

        Calls.Add(texts);

        var count = _answerWith ?? texts.Count;

        return Task.FromResult<IReadOnlyList<float[]>>([.. Enumerable.Range(0, count).Select(At)]);
    }

    /// <summary>A vector that depends only on its position, so a test can find it again.</summary>
    private float[] At(int position)
        => [.. Enumerable.Range(0, _width).Select(index => (position * _width) + index)];
}
