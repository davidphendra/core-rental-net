namespace CoreRentalNet.Modules.Catalog.Application.Contracts;

/// <summary>
/// The catalogue's name search, ranked: which of the products it is given best match what was typed.
/// </summary>
/// <remarks>
/// <para>
/// <b>It is the sibling of <see cref="IProductSimilarityService"/>, and the difference is the question.</b>
/// That one asks which products are nearest a sentence; this one asks which products a word matches, and in
/// what order. Both are handed what the caller may see, and neither may widen it.
/// </para>
/// <para>
/// <b>It is a port because the catalogue owns no index.</b> The module is an in-memory snapshot with no
/// database, and ranking is what needs one — so the index that answers this is built and owned outside the
/// module, exactly as the vector index is.
/// </para>
/// </remarks>
public interface IProductNameSearchService
{
    /// <summary>
    /// The products from <paramref name="productsMatchingTheFilters"/> whose name matches
    /// <paramref name="searchText"/>, best match first. Products that do not match are left out.
    /// </summary>
    /// <remarks>
    /// An empty <paramref name="searchText"/> is not an empty answer: it is every product the caller had
    /// already narrowed to, in catalogue order — which is how the store, the picker and the panel all open.
    /// </remarks>
    Task<IReadOnlyList<ProductView>> FindBestMatchesAsync(
        IReadOnlyList<ProductView> productsMatchingTheFilters,
        string searchText);
}
