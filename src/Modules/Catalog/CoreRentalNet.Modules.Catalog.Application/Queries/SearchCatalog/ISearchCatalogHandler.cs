using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Modules.Catalog.Application.Queries.SearchCatalog;

/// <summary>
/// The catalogue page for one category and one search.
/// </summary>
/// <remarks>
/// The operation is the abstraction: a caller asks for a page, and how it is produced is the
/// application's business. Naming it after the operation rather than the mechanism is what lets the
/// UI depend on it without knowing which service answers.
/// </remarks>
public interface ISearchCatalogHandler
{
    IReadOnlyList<ProductView> Handle(SearchCatalogQuery query);
}
