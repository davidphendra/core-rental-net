namespace CoreRentalNet.Modules.Catalog.UnitTests;

/// <summary>The seven sample rows the catalog tests read from, written as the file would hold them.</summary>
internal static class SampleCatalog
{
    /// <summary>
    /// One row per category, five accessories covering every subcategory, and two popular badges.
    /// A subcategory is absent on the chair and the desk, which is the shape the loader has to accept.
    /// </summary>
    public const string SevenRows = """
        [
          { "skuNo": "CHA0001", "name": "Chair CHA0001", "category": "chair", "pricePerMonth": 400000, "description": "A chair.", "image": "/images/chair.svg", "badge": "popular" },
          { "skuNo": "DSK0001", "name": "Desk DSK0001", "category": "desk", "pricePerMonth": 800000, "description": "A desk.", "image": "/images/desk.svg" },
          { "skuNo": "MON0001", "name": "Monitor MON0001", "category": "accessory", "subCategory": "monitor", "pricePerMonth": 300000, "description": "A monitor.", "image": "/images/monitor.svg" },
          { "skuNo": "LMP0001", "name": "Lamp LMP0001", "category": "accessory", "subCategory": "lamp", "pricePerMonth": 120000, "description": "A lamp.", "image": "/images/lamp.svg" },
          { "skuNo": "PLT0001", "name": "Plant PLT0001", "category": "accessory", "subCategory": "plant", "pricePerMonth": 200000, "description": "A plant.", "image": "/images/plant.svg", "badge": "popular" },
          { "skuNo": "CFE0001", "name": "Coffee CFE0001", "category": "accessory", "subCategory": "coffee", "pricePerMonth": 750000, "description": "A coffee machine.", "image": "/images/coffee.svg" },
          { "skuNo": "BBG0001", "name": "Beanbag BBG0001", "category": "accessory", "subCategory": "beanbag", "pricePerMonth": 350000, "description": "A bean bag.", "image": "/images/beanbag.svg" }
        ]
        """;
}
