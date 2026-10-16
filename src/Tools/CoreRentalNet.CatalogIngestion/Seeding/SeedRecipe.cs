namespace CoreRentalNet.CatalogIngestion.Seeding;

/// <summary>The recipe row a seed carries: what built the vectors beside it.</summary>
internal sealed record SeedRecipe(
    string ModelId,
    int Width,
    string Composition,
    string CatalogueHash,
    string CreatedAt);
