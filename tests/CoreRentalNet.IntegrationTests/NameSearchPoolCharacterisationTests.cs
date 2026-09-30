using AwesomeAssertions;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Infrastructure;
using CoreRentalNet.Modules.Discovery.Infrastructure.NameSearch;
using Xunit;

namespace CoreRentalNet.IntegrationTests;

/// <summary>
/// What a name-search pool can and cannot do, measured over the real catalogue — the case for a reranker, and
/// how wide its pool has to be for one to have anything to work with.
/// </summary>
/// <remarks>
/// <para>
/// <b>Measured, and kept because the numbers decide a design.</b> Each need below comes from
/// <see cref="CatalogEmbeddingFixture"/> with the product it must find. Two arms are run against the real
/// 205-product catalogue, and the two together are the finding:
/// </para>
/// <list type="table">
/// <item><description><b>A broad term</b> (the component's own category word): the pool becomes 22–30 of the
/// category and the expected product lands at <b>#14 to #30</b> — present, and far too deep for a bound that keeps
/// a composer's context sane.</description></item>
/// <item><description><b>Two narrow terms</b> (what a requirement expansion produces): the expected product is
/// within the <b>top fifteen for every need it can be found for at all</b> — #1, #1, #1, #2, #4, #5, #11 — which
/// is why the pool's bound is fifteen rather than the thirty a single broad term would need.</description></item>
/// </list>
/// <para>
/// <b>The index reads two fields, and that is what makes the second arm work.</b> Before the description was
/// indexed, two of these needs were unreachable by <i>any</i> term — the plant and the coffee machine are named
/// without their own category word — and the expected bean bag sat at #17 of 26 under terms that matched its
/// whole category. Indexing the description took the unreachable count to zero and moved that bean bag to #1.
/// </para>
/// <para>
/// <b>The order is still not a relevance order, which is the reranker's whole case.</b> Four of the eight needs
/// have their product outside a three-product window even under narrow terms, and no term both recalls and ranks.
/// </para>
/// <para>
/// <para>
/// <b>The field weight these numbers were taken at was swept separately.</b>
/// <see cref="NameFieldScoreBoostSweepTests"/> measures the same needs at three weights and finds the weight is
/// not the lever — so the pool's bound is a property of the terms and the two indexed fields, and not of a
/// tuning number.
/// </para>
/// This is a canary rather than a contract: if the index or its scoring is ever changed again, these numbers move
/// and this test says so, because the reranker's cost is derived from them.
/// </para>
/// </remarks>
public sealed class NameSearchPoolCharacterisationTests
{
    /// <summary>One category word per need — the widest term an expansion could reasonably carry.</summary>
    private static readonly IReadOnlyDictionary<string, string> BroadTermForNeed = new Dictionary<string, string>
    {
        ["a desk i can raise to stand while gaming"] = "desk",
        ["my back hurts after sitting all day"] = "chair",
        ["a gold arched floor lamp beside my desk"] = "lamp",
        ["a high resolution screen for editing photos"] = "monitor",
        ["a colourful stained glass lamp for a reading nook"] = "lamp",
        ["something green that survives a dark corner"] = "plant",
        ["i want to make espresso at my desk"] = "coffee",
        ["a big furry bean bag to sink into"] = "bean",
    };

    /// <summary>Two terms per need — what the rephraser is asked to produce.</summary>
    private static readonly IReadOnlyDictionary<string, string[]> TermsForNeed = new Dictionary<string, string[]>
    {
        ["a desk i can raise to stand while gaming"] = ["standing desk", "adjustable desk"],
        ["my back hurts after sitting all day"] = ["ergonomic chair", "office chair"],
        ["a gold arched floor lamp beside my desk"] = ["floor lamp", "arched lamp"],
        ["a high resolution screen for editing photos"] = ["4k monitor", "computer monitor"],
        ["a colourful stained glass lamp for a reading nook"] = ["stained glass lamp", "table lamp"],
        ["something green that survives a dark corner"] = ["indoor plant", "potted plant"],
        ["i want to make espresso at my desk"] = ["espresso machine", "coffee machine"],
        ["a big furry bean bag to sink into"] = ["oversized bean bag", "furry bean bag"],
    };

    [Fact] // the pool's bound is derived from this: fifteen covers every need the search can answer
    public async Task Two_narrow_terms_put_the_expected_product_within_the_pool_for_every_need_but_one()
    {
        var found = await SearchEachNeed(TermsForNeed);

        found.Count(result => result.Position > 0).Should().Be(
            7,
            "one need's product is still unreachable by its own terms, and that is a term problem rather than a "
            + "pool-size one");

        found.Where(result => result.Position > 0).Should().OnlyContain(
            result => result.Position <= 15,
            "the bound is fifteen because every product the search can find is inside it");
    }

    [Fact] // and the order is not a relevance order, which is what the reranker exists to fix
    public async Task One_broad_term_still_buries_the_product()
    {
        var found = await SearchEachNeed(BroadTermForNeed.ToDictionary(
            need => need.Key,
            need => new[] { need.Value }));

        found.Count(result => result.Position > 8).Should().BeGreaterThanOrEqualTo(
            5,
            "a single broad term recalls the category and ranks by name-likeness, so the product a need describes "
            + "is not near the top");
    }

    /// <summary>Each need searched by the terms given for it, with where its product landed.</summary>
    private static async Task<IReadOnlyList<NeedSearchOutcome>> SearchEachNeed(
        IReadOnlyDictionary<string, string[]> termsForNeed)
    {
        var fixture = CatalogEmbeddingFixture.Load();
        var catalogue = new ProductCatalogService(RepoRoot.Combine("src", "shared", "data", "products.json"), null);
        var nameSearch = new LiftiProductNameSearchService(catalogue);

        var outcomes = new List<NeedSearchOutcome>();

        foreach (var query in fixture.Queries)
        {
            var category = Enum.Parse<CatalogCategory>(query.Category!, ignoreCase: true);
            var subCategory = query.SubCategory is null
                ? (CatalogSubCategory?)null
                : Enum.Parse<CatalogSubCategory>(query.SubCategory, ignoreCase: true);

            var eligible = catalogue.Search(category, subCategory);
            var found = await nameSearch.FindBestMatchesForExpandedSearchTermsAsync(
                eligible, termsForNeed[query.Phrase]);

            outcomes.Add(new NeedSearchOutcome(
                found.Count,
                found.ToList().FindIndex(product => product.Sku == query.ExpectedSku) + 1));
        }

        return outcomes;
    }

    /// <summary>One need's outcome: how big the pool was, and where the product it must find landed in it.</summary>
    private sealed record NeedSearchOutcome(int PoolSize, int Position);
}
