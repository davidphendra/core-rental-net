using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Host.Presentation;

/// <summary>Projects the module's published view onto the fields a machine caller reads.</summary>
/// <remarks>
/// Derived from <see cref="ProductView"/> rather than listed by hand from the module, so a field added
/// to the record is a decision about this projection rather than something that silently appears in it
/// - or silently fails to.
/// </remarks>
internal static class CompactCatalogProjection
{
    /// <param name="page">The rows this answer carries.</param>
    /// <param name="total">How many rows matched, which is more than the page when it was capped.</param>
    /// <param name="currency">The currency every price on the page is stated in.</param>
    public static CompactCatalogCollection Of(IReadOnlyList<ProductView> page, int total, string currency)
    {
        ArgumentNullException.ThrowIfNull(page);

        return new CompactCatalogCollection([.. page.Select(Item)], page.Count, total, currency);
    }

    /// <summary>One product as this projection carries it.</summary>
    /// <remarks>
    /// The price and its currency both come from the module's <c>Money</c>, so this projection converts
    /// nothing: it chooses which fields cross and leaves the arithmetic where the module keeps it.
    /// </remarks>
    public static CompactCatalogItem Item(ProductView product)
    {
        ArgumentNullException.ThrowIfNull(product);

        return new CompactCatalogItem(
            product.Sku,
            product.Category,
            product.Name,
            product.SubCategory,
            product.Description,
            product.MonthlyPrice.Amount,
            product.MonthlyPrice.Currency);
    }

    /// <summary>The compact envelope the catalogue's <b>tools</b> publish: the API's, with descriptions trimmed.</summary>
    /// <remarks>
    /// It exists because a tool answer is spent on a model's context rather than shown to a page. The API's own
    /// compact answer is deliberately <b>not</b> capped: it is a published shape, asserted field by field, and a
    /// cap added for the tools would change the answer a page is written against.
    /// </remarks>
    /// <param name="page">The rows this answer carries.</param>
    /// <param name="total">How many rows matched, which is more than the page when it was capped.</param>
    /// <param name="currency">The ISO 4217 code every price on the page is stated in.</param>
    /// <param name="maximumDescriptionCharacterCount">How long a description may be in this answer.</param>
    public static CompactCatalogCollection ForTools(
        IReadOnlyList<ProductView> page,
        int total,
        string currency,
        int maximumDescriptionCharacterCount)
        => new(
            [.. page.Select(product => Item(product) with
            {
                Description = Trimmed(product.Description, maximumDescriptionCharacterCount),
            })],
            page.Count,
            total,
            currency);

    /// <summary>A description cut to a length, at a word boundary, so a trimmed one does not read as a typo.</summary>
    /// <remarks>
    /// The cut is made at a word boundary for the same reason the agent's reranker input is: a description cut
    /// mid-word reads as a typo, and this text is what a reading of a product is made from.
    /// </remarks>
    public static string Trimmed(string description, int maximumDescriptionCharacterCount)
    {
        ArgumentNullException.ThrowIfNull(description);

        if (description.Length <= maximumDescriptionCharacterCount)
        {
            return description;
        }

        var lastWordBoundary = description.LastIndexOf(' ', maximumDescriptionCharacterCount);

        return lastWordBoundary <= 0
            ? description[..maximumDescriptionCharacterCount]
            : description[..lastWordBoundary];
    }

    /// <summary>The currency a catalogue's prices are stated in, which an envelope states once.</summary>
    /// <remarks>
    /// Read from the catalogue rather than written here, and from the whole of it rather than from the rows
    /// that matched: an empty result still has a currency, and a filtered result must not decide what it is.
    /// The loader refuses an empty file and refuses a row priced in anything but the settlement currency, so
    /// the fallback is unreachable - it is there so that a broken invariant is answered rather than thrown
    /// at a caller.
    /// </remarks>
    public static string CurrencyOf(IReadOnlyList<ProductView> catalogue)
    {
        ArgumentNullException.ThrowIfNull(catalogue);

        return catalogue.Count == 0 ? Currencies.Idr : catalogue[0].MonthlyPrice.Currency;
    }
}
