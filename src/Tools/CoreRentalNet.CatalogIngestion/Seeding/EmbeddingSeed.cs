namespace CoreRentalNet.CatalogIngestion.Seeding;

/// <summary>The whole of what a seed migration carries: the recipe and every stored passage.</summary>
internal sealed record EmbeddingSeed(SeedRecipe Recipe, IReadOnlyList<SeedVector> Vectors);
