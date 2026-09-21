using CoreRentalNet.Modules.Discovery.Application.Embeddings;

namespace CoreRentalNet.Modules.Discovery.UnitTests;

/// <summary>
/// An embedding deployment a test decides the behaviour of.
/// </summary>
/// <remarks>
/// It records what it was asked to embed, which is how the "the sentence goes verbatim" rule is proved: the
/// only honest way to show a string was not rewritten is to look at what the deployment was given.
/// </remarks>
internal sealed class StubEmbeddingClient(Func<IReadOnlyList<string>, IReadOnlyList<float[]>> answer) : IEmbeddingClient
{
    /// <summary>Every batch of texts this client was asked for, in order.</summary>
    public List<IReadOnlyList<string>> Calls { get; } = [];

    public Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(texts);

        Calls.Add(texts);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(answer(texts));
    }
}
