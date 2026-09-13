using CoreRentalNet.Modules.Catalog.Application.Catalog;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Domain;

namespace CoreRentalNet.Modules.Catalog.Application.Queries;

/// <summary>
/// Lists products, optionally narrowed by category, subcategory and what the customer typed. This is
/// the query behind the Builder's category tabs, its slot picker and the Store page. Results preserve
/// catalog order.
/// </summary>
public sealed record GetCatalogPage(
    CatalogCategory? Category = null,
    CatalogSubCategory? SubCategory = null,
    string? Search = null);

/// <summary>
/// The catalog page for one category and one search.
/// </summary>
/// <remarks>
/// The operation is the abstraction: a caller asks for a page, and how it is produced is the
/// application's business. Naming it after the operation rather than the mechanism is what lets the
/// UI depend on it without knowing a handler class exists.
/// </remarks>
public interface IGetCatalogPage
{
    IReadOnlyList<ProductListItem> Handle(GetCatalogPage query);
}

public sealed class GetCatalogPageHandler(IProductCatalog catalog) : IGetCatalogPage
{
    public IReadOnlyList<ProductListItem> Handle(GetCatalogPage query)
    {
        ArgumentNullException.ThrowIfNull(query);

        IEnumerable<Product> source = catalog.All;

        if (query.Category is { } category)
        {
            source = source.Where(product => product.Category == category.ToDomain());
        }

        if (query.SubCategory is { } subCategory)
        {
            source = source.Where(product => product.SubCategory == subCategory.ToDomain());
        }

        if (SearchTerm(query.Search) is { } term)
        {
            source = source.Where(product => Matches(product, term));
        }

        return source.Select(product => product.ToListItem()).ToArray();
    }

    private static string? SearchTerm(string? search)
        => string.IsNullOrWhiteSpace(search) ? null : search.Trim();

    /// <summary>
    /// A product matches on its name or on the kind of thing it is, in the words the navigation and
    /// the group headings use for it.
    /// </summary>
    /// <remarks>
    /// A name alone would be useless here, and the catalog is the evidence: the lamps are
    /// "Pererenan Clip Light", the desks "Canggu Bamboo", the monitors "Nusa Dua 27&quot; Touch".
    /// Nobody looking for a lamp types "pererenan", and "lamp" would return nothing at all while the
    /// heading above those six products says Lamps.
    /// </remarks>
    private static bool Matches(Product product, string term)
        => product.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
           || KindOf(product).Contains(term, StringComparison.OrdinalIgnoreCase);

    /// <summary>What kind of thing this is: "Accessories Lamps", or "Desks" for a desk.</summary>
    private static string KindOf(Product product)
    {
        var category = CatalogLabels.ForCategory(product.Category.ToContract());

        return product.SubCategory is { } subCategory
            ? $"{category} {CatalogLabels.ForSubCategory(subCategory.ToContract())}"
            : category;
    }
}
