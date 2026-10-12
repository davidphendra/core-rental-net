namespace CoreRentalNet.CatalogPopulate.Tests;

/// <summary>Builds a settings value for tests, overriding only what the test cares about.</summary>
internal static class TestSettings
{
    public static CatalogPopulateSettings Build(
        Mode mode = Mode.Populate,
        string compactInput = "compact.jsonl",
        string catalogPath = "products.json",
        string vendoredImagesPath = "images",
        string metaPath = "meta.jsonl",
        string reviewPath = "reviews.jsonl",
        int targetTotal = 400,
        int reviewSampleSize = 20)
        => new(
            mode,
            compactInput,
            compactInput,
            catalogPath,
            vendoredImagesPath,
            metaPath,
            metaPath,
            metaPath,
            metaPath,
            reviewPath,
            reviewPath,
            reviewPath,
            reviewPath,
            "http://localhost:8080/v1",
            "test-model",
            reviewSampleSize,
            targetTotal);
}
