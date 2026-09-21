namespace CoreRentalNet.CatalogIngestion.Tests;

/// <summary>A small catalogue, written the way products.json holds it.</summary>
/// <remarks>
/// Three rows, one per shape the loader distinguishes: a desk and a chair with no subcategory, and an
/// accessory that requires one. Every row carries metadata, because the loader refuses a row without it.
/// </remarks>
internal static class CatalogJson
{
    public const string ThreeRows = """
        [
          { "skuNo": "DSK0001", "name": "Desk DSK0001", "category": "desk", "pricePerMonth": 800000, "description": "A height-adjustable desk.", "image": "/images/desk.svg",
            "metadata": { "tags": ["desk", "standing"], "attributes": { "type": "sit-stand", "colour": "oak" }, "bestFor": ["all-day use"], "notFor": ["outdoor use"] } },
          { "skuNo": "CHA0001", "name": "Chair CHA0001", "category": "chair", "pricePerMonth": 400000, "description": "A mesh-backed chair.", "image": "/images/chair.svg",
            "metadata": { "tags": ["chair", "mesh-back"], "attributes": { "back": "mesh" }, "bestFor": ["deep work"], "notFor": [] } },
          { "skuNo": "MON0001", "name": "Monitor MON0001", "category": "accessory", "subCategory": "monitor", "pricePerMonth": 300000, "description": "A 4K monitor.", "image": "/images/monitor.svg",
            "metadata": { "tags": ["monitor", "4k"], "attributes": { "resolution": "4K" }, "bestFor": ["coding"], "notFor": [] } }
        ]
        """;

    /// <summary>A valid file that holds no products, which the loader refuses before the pipeline sees it.</summary>
    public const string NoRows = "[]";
}
