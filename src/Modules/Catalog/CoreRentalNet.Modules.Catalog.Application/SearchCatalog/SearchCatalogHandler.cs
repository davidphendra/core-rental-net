using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Modules.Catalog.Application.SearchCatalog;

/// <summary>Answers <see cref="ISearchCatalogHandler"/> through the catalogue service.</summary>
public sealed class SearchCatalogHandler(IProductCatalog catalog) : ISearchCatalogHandler
{
    public IReadOnlyList<ProductView> Handle(SearchCatalogQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        return catalog.Search(query.Category, query.SubCategory, query.Search);
    }
}
