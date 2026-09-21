namespace CoreRentalNet.CatalogIngestion.Storage;

/// <summary>One chunk's vector, with the product identity that every one of that product's rows carries.</summary>
/// <remarks>
/// <b>It holds no key and no timestamp.</b> The store gives each row its own new identity and stamps the
/// run's time, because those are properties of the write rather than of the chunk — a row has no identity
/// until it is stored, and every row of one run shares that run's moment.
/// </remarks>
internal sealed record ProductEmbeddingRow(string SkuNo, string Name, string Description, float[] Embedding);
