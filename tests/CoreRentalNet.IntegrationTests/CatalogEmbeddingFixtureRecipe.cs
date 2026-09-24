namespace CoreRentalNet.IntegrationTests;

/// <summary>
/// What the fixture's vectors were built from, mirroring the ingestion tool's recipe row.
/// </summary>
/// <remarks>
/// The guard test holds the hash against the catalogue file, so a fixture that no longer describes the
/// catalogue that is here fails loudly instead of quietly asserting against products that have changed.
/// </remarks>
internal sealed record CatalogEmbeddingFixtureRecipe(
    string ModelId,
    int Width,
    string Composition,
    string CatalogueHash);
