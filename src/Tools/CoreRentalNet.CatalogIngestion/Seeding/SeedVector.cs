namespace CoreRentalNet.CatalogIngestion.Seeding;

/// <summary>One stored passage, ready to be written as a row of the seed migration.</summary>
/// <remarks>
/// <b>The vector is held as the hex of its stored blob, not as floats.</b> The file's bytes are what the
/// seed must reproduce, so re-encoding them through <see cref="CoreRentalNet.BuildingBlocks.Infrastructure.Vectors.VectorBlob"/>
/// would be a round trip that can only lose something. The hex is the same bytes in the form a SQLite blob
/// literal takes.
/// </remarks>
internal sealed record SeedVector(
    string SkuNo,
    string EmbeddingHex,
    string Id,
    string Name,
    string Description,
    string CreatedAt);
