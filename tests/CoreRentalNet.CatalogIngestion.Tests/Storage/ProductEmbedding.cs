namespace CoreRentalNet.CatalogIngestion.Tests.Storage;

/// <summary>One stored embedding passage of a product, with the blob already decoded.</summary>
internal sealed record ProductEmbedding(
    string Id,
    string SkuNo,
    string Name,
    string Description,
    float[] Embedding,
    string CreatedAt);
