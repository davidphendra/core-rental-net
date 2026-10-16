namespace CoreRentalNet.Modules.Catalog.Application.Contracts;

/// <summary>How many rows a scoped query returns, and the most it will ever return.</summary>
/// <remarks>
/// A ceiling exists so a caller cannot turn a scoped query back into a catalogue read by passing a large
/// number. It is larger than the biggest scope (the accessory subcategories and the two categories), so the
/// cap never truncates a real scope; it only stops a caller asking for more.
/// </remarks>
public static class CatalogPageLimits
{
    /// <summary>The rows a query returns when the caller names no size.</summary>
    public const int Default = 60;

    /// <summary>The most rows a query will return, whatever the caller asks for.</summary>
    public const int Maximum = 200;
}
