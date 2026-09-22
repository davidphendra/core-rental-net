namespace CoreRentalNet.Modules.Catalog.UnitTests;

/// <summary>The seven sample rows the catalogService tests read from, written as the file would hold them.</summary>
internal static class SampleCatalog
{
    /// <summary>
    /// One row per category, five accessories covering every subcategory, and two popular badges.
    /// A subcategory is absent on the chair and the desk, which is the shape the loader has to accept.
    /// Every row carries metadata, because a row without it is refused; the malformed cases are
    /// asserted in <c>MetadataRuleTests</c> rather than here.
    /// </summary>
    public const string SevenRows = """
        [
          { "skuNo": "CHA0001", "name": "Chair CHA0001", "category": "chair", "pricePerMonth": 400000, "description": "A chair.", "image": "/images/chair.svg", "badge": "popular",
            "metadata": { "tags": ["chair", "mesh-back", "budget"], "attributes": { "type": "task", "back": "mesh" }, "bestFor": ["deep work"], "notFor": ["outdoor use"] } },
          { "skuNo": "DSK0001", "name": "Desk DSK0001", "category": "desk", "pricePerMonth": 800000, "description": "A desk.", "image": "/images/desk.svg",
            "metadata": { "tags": ["desk", "standing"], "attributes": { "type": "sit-stand" }, "bestFor": ["all-day use"], "notFor": ["outdoor use"] } },
          { "skuNo": "MON0001", "name": "Monitor MON0001", "category": "accessory", "subCategory": "monitor", "pricePerMonth": 300000, "description": "A monitor.", "image": "/images/monitor.svg",
            "metadata": { "tags": ["monitor", "4k"], "attributes": { "resolution": "4K" }, "bestFor": ["coding"], "notFor": ["competitive gaming"] } },
          { "skuNo": "LMP0001", "name": "Lamp LMP0001", "category": "accessory", "subCategory": "lamp", "pricePerMonth": 120000, "description": "A lamp.", "image": "/images/lamp.svg",
            "metadata": { "tags": ["lamp", "warm"], "attributes": { "quality": "warm" }, "bestFor": ["late sessions"], "notFor": ["a large room"] } },
          { "skuNo": "PLT0001", "name": "Plant PLT0001", "category": "accessory", "subCategory": "plant", "pricePerMonth": 200000, "description": "A plant.", "image": "/images/plant.svg", "badge": "popular",
            "metadata": { "tags": ["plant", "low-care"], "attributes": { "care": "low" }, "bestFor": ["a bright corner"], "notFor": ["a dark room"] } },
          { "skuNo": "CFE0001", "name": "Coffee CFE0001", "category": "accessory", "subCategory": "coffee", "pricePerMonth": 750000, "description": "A coffee machine.", "image": "/images/coffee.svg",
            "metadata": { "tags": ["coffee", "espresso"], "attributes": { "type": "espresso" }, "bestFor": ["a small team"], "notFor": ["a large cafe"] } },
          { "skuNo": "BBG0001", "name": "Beanbag BBG0001", "category": "accessory", "subCategory": "beanbag", "pricePerMonth": 350000, "description": "A bean bag.", "image": "/images/beanbag.svg",
            "metadata": { "tags": ["beanbag", "lounge"], "attributes": { "type": "lounge" }, "bestFor": ["a reading corner"], "notFor": ["an upright desk"] } }
        ]
        """;
}
