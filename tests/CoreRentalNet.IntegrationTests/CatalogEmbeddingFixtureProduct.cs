namespace CoreRentalNet.IntegrationTests;

/// <summary>
/// One curated product in the fixture: its catalogue facts and the vector of its rendered text.
/// </summary>
/// <param name="Vector">The stored bytes, base64 in the fixture and decoded here. One passage per product.</param>
internal sealed record CatalogEmbeddingFixtureProduct(
    string Sku,
    string Kind,
    string Name,
    string Category,
    string? SubCategory,
    int PricePerMonth,
    string TextHash,
    byte[] Vector);
