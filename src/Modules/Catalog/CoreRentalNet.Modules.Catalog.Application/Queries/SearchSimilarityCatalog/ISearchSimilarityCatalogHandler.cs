using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Modules.Catalog.Application.Queries.SearchSimilarityCatalog;

/// <summary>
/// The catalogue ordered by how near each product is to a sentence.
/// </summary>
/// <remarks>
/// One operation, asynchronous because embedding the sentence is a network call. The operation is the
/// abstraction: a caller asks for the nearest products, and how they are scored is this module's business.
/// </remarks>
public interface ISearchSimilarityCatalogHandler
{
    /// <summary>The products nearest the sentence, nearest first.</summary>
    Task<IReadOnlyList<ProductView>> HandleAsync(
        SearchSimilarityCatalogQuery query,
        CancellationToken cancellationToken);
}
