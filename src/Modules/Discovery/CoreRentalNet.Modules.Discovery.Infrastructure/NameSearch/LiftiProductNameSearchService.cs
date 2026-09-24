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
    /// <summary>The one field a product is found by. A name search finds names.</summary>
    private const string NameField = "name";

    private readonly IProductCatalogService _catalogService;

    private IReadOnlyDictionary<string, ProductView> _productsBySku;

    private IFullTextIndex<string>? _nameSearchIndex;

    public LiftiProductNameSearchService(IProductCatalogService catalogueService)
    {
        ArgumentNullException.ThrowIfNull(catalogueService);

        _catalogService = catalogueService;

        _productsBySku = _catalogService.All.ToDictionary(product => product.Sku, StringComparer.OrdinalIgnoreCase);
    }

    private async Task InitializeProduct()
    {
        if (_nameSearchIndex == null)
        {
            _nameSearchIndex = new FullTextIndexBuilder<string>()
                .WithObjectTokenization<ProductView>(product => product
                    .WithKey(entry => entry.Sku)
                    .WithField(NameField, entry => entry.Name))
                .Build();

            await _nameSearchIndex.AddRangeAsync(_catalogService.All);
        }
    }

    public async Task<IReadOnlyList<ProductView>> FindBestMatchesAsync(
        IReadOnlyList<ProductView> productsMatchingTheFilters,
        string searchText)
    {
        ArgumentNullException.ThrowIfNull(productsMatchingTheFilters);

        if (string.IsNullOrWhiteSpace(searchText))
        {
            return productsMatchingTheFilters;
        }

        await InitializeProduct();

        var skusMatchingTheFilters = productsMatchingTheFilters
            .Select(product => product.Sku)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var words = WordsOf(searchText);

        var wholeWords = SearchProductByName(
            QueryFor(words, lastWordIsPrefix: false),
            skusMatchingTheFilters);
        var wordStarts = SearchProductByName(
            QueryFor(words, lastWordIsPrefix: true),
            skusMatchingTheFilters);

        var alreadyAnswered = wholeWords
            .Select(product => product.Sku)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return [.. wholeWords, .. wordStarts.Where(product => !alreadyAnswered.Contains(product.Sku))];
    }

    /// <summary>The products one query matched, best score first and ties by SKU.</summary>
    /// <remarks>
    /// Ordering is stated here rather than inherited: a caller that depended on the index's own iteration order
    /// would be depending on something nobody promised, and equal scores would come back differently between
    /// runs.
    /// </remarks>
    private IReadOnlyList<ProductView> SearchProductByName(
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
    /// The query for one reading of what was typed: every word must appear, and the last may be a word start.
    /// </summary>
    /// <remarks>
    /// <b>The query is built rather than parsed.</b> Handing the text to a query parser would make what a
    /// customer typed into syntax, so a stray operator or wildcard would search for something nobody asked for.
    /// Every word is added as a term instead, which is why the text can only ever be a term.
    /// </remarks>
    private IQuery QueryFor(IReadOnlyList<string> words, bool lastWordIsPrefix)
        => AddWords(_nameSearchIndex?.Query()!, words, position: 0, lastWordIsPrefix).Build();

    private static FluentQueryBuilder<string> AddWords(
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
            : AddWords(term.And, words, position + 1, lastWordIsPrefix);
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

    private static IReadOnlyList<string> WordsOf(string searchText)
        => searchText.Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
