using System.ClientModel;
using CoreRentalNet.BuildingBlocks.Application.Embeddings;
using OpenAI;
using OpenAI.Embeddings;

namespace CoreRentalNet.BuildingBlocks.Infrastructure.Embeddings;

/// <summary>
/// An embedding server that speaks the OpenAI protocol but is not Azure — the local one both roots use.
/// </summary>
/// <remarks>
/// <para>
/// <b>No credential and no width request.</b> A local server authenticates nobody, so the key the client
/// requires is a constant that is not a secret; and such a server ignores the OpenAI <c>dimensions</c> option,
/// answering at the model's native width. Asking for a width would therefore record a number nothing honoured,
/// so the width is configuration that the caller <b>verifies</b> against the answer instead.
/// </para>
/// <para>
/// <b>It builds no transport of its own.</b> The <see cref="EmbeddingClient"/> is handed to it, and
/// <see cref="Build"/> is a composition root's one line for constructing one. A class that constructs its own
/// client cannot be exercised without a live server; this one can be driven through a stand-in transport.
/// </para>
/// <para>
/// <b>It lives in the shared kernel because both composition roots embed with it:</b> the ingestion tool embeds
/// a catalogue's chunks, and the application embeds every customer's sentence. The two vectors are compared
/// with each other at query time, so they have to come from the same protocol, the same server and the same
/// model — which is an argument for one adapter rather than two that agree until one of them is edited.
/// </para>
/// </remarks>
public sealed class OpenAiCompatibleEmbeddingRepository : IEmbeddingRepository
{
    /// <summary>The key the client requires and the server ignores.</summary>
    /// <remarks>
    /// <b>Not a secret and not configuration.</b> A local server authenticates nobody, and the OpenAI client
    /// will not be constructed without some key. It is named so a reader can see it is a placeholder rather
    /// than a credential that reached the source; a server that checks a specific key needs a real one, which
    /// would be a new setting and not this constant.
    /// </remarks>
    private const string PlaceholderApiKey = "local";

    private readonly EmbeddingClient _embeddingClient;

    /// <summary>The adapter over one embedding client.</summary>
    /// <param name="embeddingClient">The client to send through, built by <see cref="Build"/> or by a test.</param>
    public OpenAiCompatibleEmbeddingRepository(EmbeddingClient embeddingClient)
    {
        ArgumentNullException.ThrowIfNull(embeddingClient);

        _embeddingClient = embeddingClient;
    }

    /// <summary>The client for one server and one of the models it serves.</summary>
    /// <param name="server">The server's base URL including its version path, e.g. http://localhost:8080/v1.</param>
    /// <param name="model">The identifier the server knows the model by.</param>
    public static IEmbeddingRepository Build(string server, string model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(server);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        var llmClient = new OpenAIClient(
            new ApiKeyCredential(PlaceholderApiKey),
            new OpenAIClientOptions { Endpoint = new Uri(server, UriKind.Absolute) });

        return new OpenAiCompatibleEmbeddingRepository(llmClient.GetEmbeddingClient(model));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<float[]>> EmbedAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(texts);

        // One call for whatever the caller sent: the server takes many inputs per call, and no caller here
        // sends more than a batch of sentences, so there is no batching to do and no partial answer.
        var answerEmbeddings = await _embeddingClient
            .GenerateEmbeddingsAsync(texts, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return [.. answerEmbeddings.Value.Select(embedding => embedding.ToFloats().ToArray())];
    }
}
