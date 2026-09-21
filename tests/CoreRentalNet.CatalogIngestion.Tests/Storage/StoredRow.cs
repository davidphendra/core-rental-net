namespace CoreRentalNet.CatalogIngestion.Tests.Storage;

/// <summary>One row as it was found in the table, with the blob already decoded.</summary>
internal sealed record StoredRow(
    string Id,
    string SkuNo,
    string Name,
    string Description,
    float[] Embedding,
    string CreatedAt);
