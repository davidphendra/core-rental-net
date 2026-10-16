namespace CoreRentalNet.Modules.Catalog.Infrastructure.Persistence;

/// <summary>
/// One catalogue row as the store holds it.
/// </summary>
/// <remarks>
/// <b>The full record, not the published <c>ProductDto</c>.</b> The store carries everything the
/// content has - including the search metadata, which only the ingestion tool reads - and the adapter
/// publishes a slice of it. Internal so no caller can reach the table through it.
/// </remarks>
internal sealed class CatalogProduct
{
    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int Category { get; set; }

    public int? SubCategory { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Image { get; set; } = string.Empty;

    public bool IsFeatured { get; set; }

    /// <summary>Where the row sat in the catalogue, so SQL can reproduce the source's own order.</summary>
    public int SortOrder { get; set; }

    public List<string> Tags { get; set; } = [];

    public List<string> BestFor { get; set; } = [];

    public List<string> NotFor { get; set; } = [];

    /// <summary>The attribute bag as the JSON text the source held. Nothing reads it at runtime.</summary>
    public string AttributesJson { get; set; } = "{}";
}
