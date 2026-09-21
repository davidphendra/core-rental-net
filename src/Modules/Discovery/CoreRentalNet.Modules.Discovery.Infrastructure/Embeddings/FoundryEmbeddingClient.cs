using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Core;
using CoreRentalNet.Modules.Discovery.Application.Embeddings;
using OpenAI.Embeddings;

namespace CoreRentalNet.Modules.Discovery.Infrastructure.Embeddings;

/// <summary>
/// The embedding deployment, reached through a Foundry project.
/// </summary>
/// <remarks>
/// <para>
/// <b>It resolves no credential and reads no configuration — the caller does both.</b> This started life in the
/// ingestion tool, on the reasoning that a credential belongs to a composition root and the tool was the only
/// thing that embedded. The application embeds too: every run embeds the customer's sentence, so the adapter
/// had to move here, and the rule it moved with is that the composition root constructs it and hands it a
/// credential. The Host and the tool each own their own; neither owns this.
/// </para>
/// <para>
/// <b>The width is requested, not accepted.</b> The deployment is asked for vectors of the declared width, so a
/// deployment that cannot oblige returns something the caller refuses — rather than an index quietly recording
/// whatever arrived.
/// </para>
/// </remarks>
public sealed class FoundryEmbeddingClient(EmbeddingClient embeddings, int width) : IEmbeddingClient
{
    /// <summary>The client for one project endpoint, deployment and width.</summary>
    /// <param name="projectEndpoint">The Foundry project endpoint.</param>
    /// <param name="deployment">The embedding deployment to call.</param>
    /// <param name="width">How many floats to ask for.</param>
    /// <param name="credential">The identity to call with, resolved by the composition root.</param>
    public static IEmbeddingClient Build(string projectEndpoint, string deployment, int width, TokenCredential credential)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectEndpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(deployment);
        ArgumentNullException.ThrowIfNull(credential);

        var project = new AIProjectClient(new Uri(projectEndpoint), credential);

        return new FoundryEmbeddingClient(project.GetProjectOpenAIClient().GetEmbeddingClient(deployment), width);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<float[]>> EmbedAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(texts);

        // One call for the whole batch: the deployment's limit is 2,048 inputs, the catalogue is 205 and a
        // request is one, so there is no batching to do and no partial answer to reconcile.
        var answer = await embeddings
            .GenerateEmbeddingsAsync(texts, new EmbeddingGenerationOptions { Dimensions = width }, cancellationToken)
            .ConfigureAwait(false);

        return [.. answer.Value.Select(embedding => embedding.ToFloats().ToArray())];
    }
}
