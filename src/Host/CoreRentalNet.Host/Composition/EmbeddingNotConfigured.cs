using CoreRentalNet.Modules.Discovery.Application.Embeddings;
using CoreRentalNet.Modules.Discovery.Application.Shortlist;

namespace CoreRentalNet.Host.Composition;

/// <summary>
/// The deployment that answers when this application has not been told where to embed.
/// </summary>
/// <remarks>
/// <para>
/// This is the same shape as <c>NoAgentConfigured</c>, and for the same reason: the failure mode of a missing
/// setting is that the feature is <b>hidden</b>, not that it is free. Registering nothing would make the
/// container fail to resolve the shortlist and the run would surface as a server error — a configuration gap
/// dressed up as a fault, which is the one thing a deployment must never be told about its own settings.
/// </para>
/// <para>
/// It throws <see cref="ShortlistUnavailableException"/> rather than returning an empty answer, so the run
/// reports itself unavailable and the customer is offered a retry. An empty shortlist would be the dangerous
/// alternative: the model would be handed no products at all, and would either refuse or invent.
/// </para>
/// </remarks>
internal sealed class EmbeddingNotConfigured : IEmbeddingClient
{
    private const string Reason =
        "No embedding deployment is configured, so no shortlist can be built. Set Discovery:ProjectEndpoint and "
        + "Discovery:Deployment, and run CoreRentalNet.CatalogIngestion against the index.";

    /// <inheritdoc />
    public Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken)
        => Task.FromException<IReadOnlyList<float[]>>(new ShortlistUnavailableException(Reason));
}
