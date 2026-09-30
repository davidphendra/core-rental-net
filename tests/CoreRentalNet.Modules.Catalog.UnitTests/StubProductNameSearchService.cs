using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Modules.Catalog.UnitTests;

/// <summary>
/// A ranker a test decides the answer of, so the handler's own rule can be asserted without a real index.
/// </summary>
/// <remarks>
/// The ranking itself is the package's and is asserted over the real catalogue elsewhere. What this stands in
/// for is the port, so the handler can be asked what it narrowed to, what it searched for, and what it did
/// with the answer.
/// </remarks>
internal sealed class StubProductNameSearchService : IProductNameSearchService
{
    /// <summary>The products the ranker will say matched, in the order it will say them.</summary>
    public IReadOnlyList<ProductView> Matches { get; init; } = [];

    /// <summary>The products the handler handed over, so a test can assert what was ranked.</summary>
    public IReadOnlyList<ProductView>? ProductsItWasGiven { get; private set; }

    /// <summary>What the handler searched for, or null when it never asked.</summary>
    public string? TypedSearchTextItWasGiven { get; private set; }

    /// <summary>The terms the handler searched for, or null when it never asked.</summary>
    public IReadOnlyList<string>? ExpandedSearchTermsItWasGiven { get; private set; }

    /// <inheritdoc />
    public Task<IReadOnlyList<ProductView>> FindBestMatchesForTypedSearchWordsAsync(
        IReadOnlyList<ProductView> productsMatchingTheFilters,
        string typedSearchText)
    {
        ProductsItWasGiven = productsMatchingTheFilters;
        TypedSearchTextItWasGiven = typedSearchText;

        return Task.FromResult(Matches);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ProductView>> FindBestMatchesForExpandedSearchTermsAsync(
        IReadOnlyList<ProductView> productsMatchingTheFilters,
        IReadOnlyList<string> expandedSearchTerms)
    {
        ProductsItWasGiven = productsMatchingTheFilters;
        ExpandedSearchTermsItWasGiven = expandedSearchTerms;

        return Task.FromResult(Matches);
    }
}
