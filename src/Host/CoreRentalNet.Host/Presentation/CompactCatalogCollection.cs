namespace CoreRentalNet.Host.Presentation;

/// <summary>
/// A compact collection answer: the items, how many there are, and the currency their prices are in.
/// </summary>
/// <remarks>
/// A separate envelope from <see cref="ApiCollection{T}"/> rather than an optional extra field on it,
/// because the two answers have different shapes and one of them is already published: adding a
/// nullable <c>currency</c> to the shared envelope would put the field in the full answer too, and the
/// full answer's shape is asserted field by field.
/// </remarks>
/// <param name="Value">The items, in the order the module published them.</param>
/// <param name="Count">How many items this answer carries.</param>
/// <param name="Currency">The ISO 4217 code every price in this answer is stated in.</param>
public sealed record CompactCatalogCollection(
    IReadOnlyList<CompactCatalogItem> Value,
    int Count,
    string Currency);
