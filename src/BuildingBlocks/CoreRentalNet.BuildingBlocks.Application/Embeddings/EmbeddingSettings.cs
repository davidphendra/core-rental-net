namespace CoreRentalNet.BuildingBlocks.Application.Embeddings;

/// <summary>What an embedding client needs, independent of where the values came from.</summary>
/// <remarks>
/// The two composition roots read their own configuration, then hand this one value to the factory. That
/// keeps the protocol choice out of the adapter and stops the tool and the application describing the same
/// client differently.
/// </remarks>
public sealed record EmbeddingSettings(
    EmbeddingProvider Provider,
    Uri Endpoint,
    string? ApiKey,
    string Model,
    int Width);
