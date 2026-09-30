using CoreRentalNet.Modules.Catalog.Application.Contracts;
using Lifti;
using Lifti.Querying;

namespace CoreRentalNet.Modules.Discovery.Infrastructure.NameSearch;

/// <summary>
/// The catalogue's name search, ranked by Lifti's Okapi BM25 over an index built from the catalogue.
/// </summary>
/// <remarks>
/// <para>
/// <b>No search algorithm is written here.</b> The scoring, the tokenisation, the stemming and the reading of
/// what was typed are the package's — its default scorer is Okapi BM25 with k1 = 1.2 and b = 0.75. What this
/// class decides is the shape of the index and the order the two questions are asked in.
/// </para>
/// <para>
/// <b>A whole-word match is answered before a word-start match, and that is not decoration.</b> The search box
/// in the panel and the picker looks on every keystroke, so a half-typed word has to match — but a search for
/// <c>hon</c> must still put the HON desks above a night light called <i>Honey Bear</i>. One rule cannot do
/// both: matching whole words only breaks the typing, and matching word starts only ranks the lamp first. Both
/// are therefore asked, and the answer is the whole-word matches followed by the word-start matches that are
/// new.
/// </para>
/// <para>
/// <b>The index is in memory and derived, so it cannot be stale.</b> It is built in the constructor from the
/// same snapshot the catalogue answers from, and rebuilt on the next start.
/// </para>
/// </remarks>
public sealed class LiftiProductNameSearchService : IProductNameSearchService
{
    /// <summary>The product's name, which is what a catalogue prints it under.</summary>
    private const string NameField = "name";

    /// <summary>The product's own description, which is where its characteristics are stated.</summary>
    /// <remarks>
    /// <b>Indexed because the name is not where a product's characteristics live.</b> Measured over the real
    /// catalogue, a need whose distinguishing words are in the description — a monitor that is 4K, a chair with
    /// lumbar support — cannot be found by its name at all, and the products a need describes land at #16 to #23
    /// of their category when the search is broad enough to contain them. The description is the text that says
    /// what a product is, so it is the text a search has to read.
    /// <para>
    /// <b>Only the expanded terms read it, never the typed search.</b> The widening exists for the agent, whose
    /// terms are phrases derived from a sentence; a search box that matched prose would answer every keystroke
    /// differently from the day before, so what a customer types is still answered by the name alone.
    /// </para>
    /// </remarks>
    private const string DescriptionField = "description";

    /// <summary>How much more a name match is worth than a description match.</summary>
    /// <remarks>
    /// <para>
    /// <b>Not one, because a description is much longer than a name.</b> Unboosted, a word that appears in many
    /// descriptions would outrank a product whose name it appears in, and what a caller typed is far more often
    /// part of a name than part of the prose. The factor keeps a name match winning without hiding a product that
    /// only its description explains.
    /// </para>
    /// <para>
    /// <b>Five, and the measurement's result is that this number is not load-bearing.</b> Swept over the real
    /// catalogue at 2, 5 and 10 (<c>NameFieldScoreBoostSweepTests</c>), it changes almost nothing: 5 and 10 answer
    /// identically — the weight saturates — and at 2 one need improves by two places while another loses one,
    /// which is one product's rank bought with another's. Reachability and the worst position are the same at all
    /// three, because what decides a pool is the terms and the two indexed fields rather than this.
    /// </para>
    /// <para>
    /// <b>Five is kept because it sits inside the range that answers the same way, not at the edge of it.</b> The
    /// weight is a constructor parameter so that measurement can be repeated, and a test pins the shipping service
    /// to the measured value: changing this constant without re-reading the sweep fails a test rather than
    /// silently re-tuning the agent's pool.
    /// </para>
    /// </remarks>
    private const double NameFieldScoreBoost = 5;

    /// <summary>The smallest weight that still prefers a name match to a description match.</summary>
    /// <remarks>
    /// A weight below one would do the opposite of what the field is for, so it is refused rather than clamped: a
    /// tuning mistake should not look like a scoring decision.
    /// </remarks>
    private const double MinimumNameFieldScoreBoost = 1;

    private readonly IProductCatalogService _catalogService;

    private readonly double _nameFieldScoreBoost;

    private IReadOnlyDictionary<string, ProductView> _productsBySku;

    private IFullTextIndex<string>? _nameSearchIndex;

    public LiftiProductNameSearchService(IProductCatalogService catalogueService)
        : this(catalogueService, NameFieldScoreBoost)
    {
    }

    /// <summary>The name search with an explicit name-field weight, so the weight can be measured.</summary>
    /// <remarks>
    /// <b>The weight is a parameter rather than a constant only so that it can be swept.</b> A tuning number that
    /// can be changed in code but not measured in a test is a number nobody can defend, and the sweep that
    /// justifies this one has to be able to build the same index at other weights and compare the answers.
    /// </remarks>
    public LiftiProductNameSearchService(IProductCatalogService catalogueService, double nameFieldScoreBoost)
    {
        ArgumentNullException.ThrowIfNull(catalogueService);

        ArgumentOutOfRangeException.ThrowIfLessThan(nameFieldScoreBoost, MinimumNameFieldScoreBoost);

        _catalogService = catalogueService;
        _nameFieldScoreBoost = nameFieldScoreBoost;

        _productsBySku = _catalogService.All.ToDictionary(product => product.Sku, StringComparer.OrdinalIgnoreCase);
    }

    private async Task InitializeProduct()
    {
        if (_nameSearchIndex == null)
        {
            _nameSearchIndex = new FullTextIndexBuilder<string>()
                .WithObjectTokenization<ProductView>(product => product
                    .WithKey(entry => entry.Sku)
                    .WithField(NameField, entry => entry.Name, scoreBoost: _nameFieldScoreBoost)
                    .WithField(DescriptionField, entry => entry.Description))
                .Build();

            await _nameSearchIndex.AddRangeAsync(_catalogService.All);
        }
    }

    public async Task<IReadOnlyList<ProductView>> FindBestMatchesForTypedSearchWordsAsync(
        IReadOnlyList<ProductView> productsMatchingTheFilters,
        string typedSearchText)
    {
        ArgumentNullException.ThrowIfNull(productsMatchingTheFilters);

        if (string.IsNullOrWhiteSpace(typedSearchText))
        {
            return productsMatchingTheFilters;
        }

        await InitializeProduct();

        var skusMatchingTheFilters = productsMatchingTheFilters
            .Select(product => product.Sku)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var words = SearchWordsIn(typedSearchText);

        var wholeWords = ProductsMatching(
            SearchQueryForTypedWords(words, lastWordIsPrefix: false),
            skusMatchingTheFilters);
        var wordStarts = ProductsMatching(
            SearchQueryForTypedWords(words, lastWordIsPrefix: true),
            skusMatchingTheFilters);

        var alreadyAnswered = wholeWords
            .Select(product => product.Sku)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return [.. wholeWords, .. wordStarts.Where(product => !alreadyAnswered.Contains(product.Sku))];
    }

    /// <inheritdoc />
    /// <remarks>
    /// <b>The terms are merged, not the query.</b> Each term already has a rule — every word of it must
    /// appear — and that rule is asked once per term, so a phrase stays a phrase. Lowering the inner
    /// conjunction to an outer one, which is what a bag of words would be, is the difference between finding a
    /// desk and finding half the catalogue's desks. Merging ranked answers is also what this class already does
    /// for the typed search, where whole-word matches are followed by the word-start matches that are new.
    /// <para>
    /// The terms are asked in the order they were given, so the answer is: everything the first term found,
    /// then whatever the second term found that is new, and so on. The caller's order is therefore the ranking
    /// between terms, which is how a primary term's matches come to be answered before a synonym's.
    /// </para>
    /// </remarks>
    public async Task<IReadOnlyList<ProductView>> FindBestMatchesForExpandedSearchTermsAsync(
        IReadOnlyList<ProductView> productsMatchingTheFilters,
        IReadOnlyList<string> expandedSearchTerms)
    {
        ArgumentNullException.ThrowIfNull(productsMatchingTheFilters);
        ArgumentNullException.ThrowIfNull(expandedSearchTerms);

        if (expandedSearchTerms.Count == 0)
        {
            return productsMatchingTheFilters;
        }

        await InitializeProduct();

        var skusMatchingTheFilters = productsMatchingTheFilters
            .Select(product => product.Sku)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var matchedProducts = new List<ProductView>();
        var alreadyAnswered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var expandedSearchTerm in expandedSearchTerms)
        {
            var productsMatchingThisTerm = ProductsMatching(
                SearchQueryForExpandedTerm(expandedSearchTerm),
                skusMatchingTheFilters);

            foreach (var product in productsMatchingThisTerm.Where(product => alreadyAnswered.Add(product.Sku)))
            {
                matchedProducts.Add(product);
            }
        }

        return matchedProducts;
    }

    /// <summary>The products one query matched, best score first and ties by SKU.</summary>
    /// <remarks>
    /// Ordering is stated here rather than inherited: a caller that depended on the index's own iteration order
    /// would be depending on something nobody promised, and equal scores would come back differently between
    /// runs.
    /// </remarks>
    private IReadOnlyList<ProductView> ProductsMatching(
        IQuery query,
        IReadOnlySet<string> skusMatchingTheFilters)
        =>
        [
            .. _nameSearchIndex == null
                ? []
                : _nameSearchIndex.Search(query)
                .Where(match => skusMatchingTheFilters.Contains(match.Key))
                .OrderByDescending(match => match.Score)
                .ThenBy(match => match.Key, StringComparer.Ordinal)
                .Select(match => _productsBySku[match.Key]),
        ];

    /// <summary>
    /// The query for what a customer typed: every word must appear in the product's name.
    /// </summary>
    /// <remarks>
    /// <b>The name field only, and that is what leaves the search box as it was.</b> The index reads descriptions as
    /// well, because a need's distinguishing words live there — but that widening exists for the expanded terms, and
    /// a search box that had begun matching prose would answer every search in the application differently. What a
    /// customer types is part of a name far more often than it is a characteristic, so the whole reading is wrapped
    /// in one field restriction, and the terms an expansion produces are the reading that is not.
    /// </remarks>
    private IQuery SearchQueryForTypedWords(IReadOnlyList<string> words, bool lastWordIsPrefix)
        => _nameSearchIndex!.Query()
            .InField(
                NameField,
                nameOnlyQuery => AddSearchWordsTo(nameOnlyQuery, words, position: 0, lastWordIsPrefix))
            .Build();

    /// <summary>The query for one expanded term: every word must appear, in the name or in the description.</summary>
    /// <remarks>
    /// <b>Every indexed field, because a term is a characteristic rather than a name.</b> An expansion produces words
    /// chosen because a sentence implied them — <i>lumbar support</i>, <i>4K</i> — and a catalogue states those in
    /// prose. The term is asked as a phrase still: every word of it must appear in one product's text.
    /// </remarks>
    private IQuery SearchQueryForExpandedTerm(string expandedSearchTerm)
        => AddSearchWordsTo(
            _nameSearchIndex?.Query()!,
            SearchWordsIn(expandedSearchTerm),
            position: 0,
            lastWordIsPrefix: false).Build();

    private static FluentQueryBuilder<string> AddSearchWordsTo(
        SearchTermFluentQueryBuilder<string> query,
        IReadOnlyList<string> words,
        int position,
        bool lastWordIsPrefix)
    {
        var isLastWord = position == words.Count - 1;
        var term = isLastWord && lastWordIsPrefix
            ? query.WildcardMatch(WordStartPatternFor(words[position]))
            : query.ExactMatch(words[position]);

        return isLastWord
            ? term
            : AddSearchWordsTo(term.And, words, position + 1, lastWordIsPrefix);
    }

    /// <summary>The pattern for a word start: the word itself, and anything that begins with it.</summary>
    /// <remarks>
    /// The two characters a pattern treats as wildcards are removed rather than escaped, because there is no
    /// escape — so a customer typing <c>100%</c> searches for 100 and everything beginning with it.
    /// </remarks>
    private static string WordStartPatternFor(string word)
        => $"{Strip(word)}{'*'}";

    private static string Strip(string word)
        => word
            .Replace("*", string.Empty, StringComparison.Ordinal)
            .Replace("%", string.Empty, StringComparison.Ordinal);

    private static IReadOnlyList<string> SearchWordsIn(string searchText)
        => searchText.Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
