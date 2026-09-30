namespace CoreRentalNet.Modules.Catalog.Application.Contracts;

/// <summary>
/// The catalogue's name search, ranked: which of the products it is given best match what was asked for.
/// </summary>
/// <remarks>
/// <para>
/// <b>It is the sibling of <see cref="IProductSimilarityService"/>, and the difference is the question.</b>
/// That one asks which products are nearest a sentence; this one asks which products a name matches, and in
/// what order. Both are handed what the caller may see, and neither may widen it.
/// </para>
/// <para>
/// <b>It is a port because the catalogue owns no index.</b> The module is an in-memory snapshot with no
/// database, and ranking is what needs one — so the index that answers this is built and owned outside the
/// module, exactly as the vector index is.
/// </para>
/// <para>
/// <b>Two questions rather than one with a mode.</b> A customer typing narrows towards the one thing they
/// mean and every word must appear; a search that has already been expanded into terms is looking for
/// alternatives and any term may match. A flag would put that difference in a value a caller can pass
/// wrongly; two named methods put it in their names.
/// </para>
/// </remarks>
public interface IProductNameSearchService
{
    /// <summary>
    /// The products from <paramref name="productsMatchingTheFilters"/> whose name matches the words a
    /// customer typed, best match first. Every word must appear; a product that does not match is left out.
    /// </summary>
    /// <remarks>
    /// An empty <paramref name="typedSearchText"/> is not an empty answer: it is every product the caller had
    /// already narrowed to, in catalogue order — which is how the store, the picker and the panel all open.
    /// </remarks>
    Task<IReadOnlyList<ProductView>> FindBestMatchesForTypedSearchWordsAsync(
        IReadOnlyList<ProductView> productsMatchingTheFilters,
        string typedSearchText);

    /// <summary>
    /// The products from <paramref name="productsMatchingTheFilters"/> whose name matches any of the terms a
    /// search was expanded into, best match first. A product that matches no term is left out.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A term is a phrase and the terms are alternatives.</b> Every word of one term must appear; a product
    /// needs to match only one term. A customer typing must not be narrowed to nothing by a word the catalogue
    /// does not use, which is exactly what would happen if the expansion's words were treated as one search.
    /// </para>
    /// <para>
    /// An empty <paramref name="expandedSearchTerms"/> is not an empty answer, exactly as an empty text is not:
    /// it is every product the caller had already narrowed to.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<ProductView>> FindBestMatchesForExpandedSearchTermsAsync(
        IReadOnlyList<ProductView> productsMatchingTheFilters,
        IReadOnlyList<string> expandedSearchTerms);
}
