using CoreRentalNet.Modules.Catalog.Application.Catalog;

namespace CoreRentalNet.Host.Presentation;

/// <summary>
/// Filters products by what the customer typed.
/// </summary>
/// <remarks>
/// <para>
/// A search is a filter over what the caller already has rather than a query, because the catalog
/// is held in memory at this size. That is also what keeps the panel's search and the picker's
/// search separate: each narrows the list its own surface was already showing and never reaches
/// outside it, so typing in one has no effect on the other.
/// </para>
/// <para>
/// A product matches on its name or on the kind of thing it is, in the words the navigation uses
/// for it. A name alone would be useless here, which the catalog is the evidence for: the lamps are
/// "Pererenan Clip Light", the desks "Canggu Bamboo", the monitors "Nusa Dua 27&quot; Touch". Nobody
/// searching for a lamp types "pererenan", and "lamp" would return nothing at all while the heading
/// above those six products says Lamps.
/// </para>
/// </remarks>
public static class CatalogSearch
{
    public static IReadOnlyList<ProductListItem> Matching(IReadOnlyList<ProductListItem> products, string? query)
    {
        ArgumentNullException.ThrowIfNull(products);

        var trimmed = query?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            return products;
        }

        return products.Where(product => Matches(product, trimmed)).ToArray();
    }

    private static bool Matches(ProductListItem product, string query)
        => product.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
           || KindOf(product).Contains(query, StringComparison.OrdinalIgnoreCase);

    /// <summary>What kind of thing this is, in the words the tabs and the group headings use.</summary>
    private static string KindOf(ProductListItem product)
    {
        var tab = CatalogTabs.All.First(candidate => CatalogTabs.CategoryFor(candidate) == product.Category);
        var kind = CatalogTabs.LabelFor(tab);

        // "Accessories Lamps", so that lamp, lamps and accessories all find the lamps.
        return product.SubCategory is { } subCategory
            ? $"{kind} {AccessoryGroups.LabelFor(subCategory)}"
            : kind;
    }
}
