namespace CoreRentalNet.CatalogIngestion.Tests.Storage;

/// <summary>The recipe row as it was found in the table.</summary>
internal sealed record StoredRecipe(
    string ModelId,
    int Width,
    string Composition,
    string CatalogueHash,
    string CreatedAt);
