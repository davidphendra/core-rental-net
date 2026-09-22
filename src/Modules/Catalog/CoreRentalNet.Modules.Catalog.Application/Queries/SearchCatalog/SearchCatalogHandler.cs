using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Modules.Catalog.Application.Queries.SearchCatalog;

/// <summary>Answers <see cref="ISearchCatalogHandler"/> through the catalogue service.</summary>
public sealed class SearchCatalogHandler(IProductCatalogService catalogService) : ISearchCatalogHandler
{
    public IReadOnlyList<ProductView> Handle(SearchCatalogQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        return catalogService.Search(query.Category, query.SubCategory, query.Search);
    }
}
