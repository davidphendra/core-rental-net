using AwesomeAssertions;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Infrastructure;
using CoreRentalNet.Modules.Discovery.Infrastructure.NameSearch;
using Xunit;
using Xunit.Abstractions;

namespace CoreRentalNet.IntegrationTests;

/// <summary>
/// Measures the one number in the name search that was chosen by taste: how much more a name match is worth than
/// a description match.
/// </summary>
/// <remarks>
/// <para>
/// <b>The weight only reaches the expanded terms, and that is why it is safe to tune.</b> The typed search box is
/// restricted to the name field, so with a single field a uniform weight cannot reorder anything — what a
/// customer sees in the store is the same at every weight. The weight decides one thing only: how deep in the
/// pool the agent's terms put the product a need describes. That is the number this sweep measures, and the pool
/// bound of fifteen is derived from it.
/// </para>
/// <para>
/// <b>Three weights, and the same catalogue.</b> Each is measured over the real 205 products with
/// <see cref="CatalogEmbeddingFixture"/> as the oracle for which product each need must find. The table is written
/// to the test output so a future change to the scoring shows up as a number rather than as an opinion, and the
/// assertions state the two properties the chosen weight must have: reachability is unaffected, and everything
/// reachable is inside the pool.
/// </para>
/// <para>
/// <b>What the sweep cannot do is make a term find what it never mentioned.</b> A product whose name and
/// description share no word with any of a need's terms is unreachable at every weight, which is why the count of
/// unreachable needs is asserted to be the same at all three.
/// </para>
/// </remarks>
public sealed class NameFieldScoreBoostSweepTests(ITestOutputHelper output)
{
    /// <summary>The weights measured: below the default, the default, and well above it.</summary>
    private static readonly double[] WeightsToMeasure = [2, 5, 10];

    /// <summary>Two terms per need — what a requirement expansion produces.</summary>
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

    [Fact] // the pool bound of fifteen is only defensible while this holds at the weight the deployment runs
    public async Task Every_reachable_need_is_inside_the_pool_at_the_weight_that_ships()
    {
        var measured = await MeasureEachWeight();

        WriteTheTable(measured);

        foreach (var outcome in measured)
        {
            outcome.Outcomes.Where(result => result.Position > 0).Should().OnlyContain(
                result => result.Position <= 15,
                $"at weight {outcome.NameFieldScoreBoost} the pool of fifteen still covers every product the "
                + "search can find");
        }
    }

    [Fact] // the weight cannot change what is reachable, only where it lands: a different count means a term moved
    public async Task The_weight_does_not_change_which_needs_are_reachable()
    {
        var measured = await MeasureEachWeight();

        measured.Select(outcome => outcome.Outcomes.Count(result => result.Position > 0)).Distinct().Should()
            .ContainSingle(
                "reachability is a property of the terms and the index's fields, so a weight that changed it would "
                + "mean it had started hiding products");
    }

    [Fact] // raising the weight cannot help: the range saturates, which is why the default is not at an edge
    public async Task Raising_the_weight_above_the_default_changes_nothing_at_all()
    {
        var measured = await MeasureEachWeight();

        PositionsAt(measured, 10).Should().Equal(
            PositionsAt(measured, 5),
            "the weight saturates by five, so a future increase has to be justified by something other than this");
    }

    [Fact] // and it barely moves the order in either direction
    public async Task The_weight_leaves_the_mean_position_where_it_was()
    {
        var measured = await MeasureEachWeight();

        foreach (var weight in measured)
        {
            var reachable = weight.Outcomes.Where(outcome => outcome.Position > 0).ToList();

            reachable.Average(outcome => outcome.Position).Should().BeInRange(
                3,
                4,
                $"at weight {weight.NameFieldScoreBoost} the weight is not the lever, so the mean stays where it is");
        }
    }

    [Fact] // the constant itself: change it and this fails, so the number cannot drift away from its measurement
    public async Task The_shipping_service_answers_as_the_measured_weight_of_five_does()
    {
        var measured = await MeasureEachWeight();

        var asShipped = await MeasureEveryNeedAsync(new LiftiProductNameSearchService(TheCatalogue()));

        asShipped.Select(outcome => outcome.Position).Should().Equal(
            PositionsAt(measured, 5),
            "the shipping default is the measured value, and moving it means re-reading this sweep");
    }

    [Fact] // and the search box: one field, one weight, so nothing a customer sees can move
    public async Task The_typed_search_box_answers_identically_at_every_weight()
    {
        var catalogue = new ProductCatalogService(
            RepoRoot.Combine("src", "shared", "data", "products.json"), null);

        var answers = new List<IReadOnlyList<string>>();

        foreach (var weight in WeightsToMeasure)
        {
            var nameSearch = new LiftiProductNameSearchService(catalogue, weight);

            answers.Add([.. (await nameSearch.FindBestMatchesForTypedSearchWordsAsync(catalogue.All, "chair"))
                .Select(product => product.Sku)]);
        }

        answers.Should().OnlyContain(
            answer => answer.SequenceEqual(answers[0]),
            "the typed search reads the name field alone, so its order cannot depend on the weight of that field");
    }

    /// <summary>Each weight measured over every need, with where the product a need describes landed.</summary>
    private static async Task<IReadOnlyList<MeasuredWeight>> MeasureEachWeight()
    {
        var catalogue = TheCatalogue();
        var measured = new List<MeasuredWeight>();

        foreach (var weight in WeightsToMeasure)
        {
            measured.Add(new MeasuredWeight(
                weight,
                await MeasureEveryNeedAsync(new LiftiProductNameSearchService(catalogue, weight))));
        }

        return measured;
    }

    /// <summary>One service, every need, and where the product each need describes landed.</summary>
    private static async Task<IReadOnlyList<NeedSearchOutcome>> MeasureEveryNeedAsync(
        LiftiProductNameSearchService nameSearch)
    {
        var fixture = CatalogEmbeddingFixture.Load();
        var catalogue = TheCatalogue();
        var outcomes = new List<NeedSearchOutcome>();

        foreach (var query in fixture.Queries)
        {
            var category = Enum.Parse<CatalogCategory>(query.Category!, ignoreCase: true);
            var subCategory = query.SubCategory is null
                ? (CatalogSubCategory?)null
                : Enum.Parse<CatalogSubCategory>(query.SubCategory, ignoreCase: true);

            var eligible = catalogue.Search(category, subCategory);
            var found = await nameSearch.FindBestMatchesForExpandedSearchTermsAsync(
                eligible, TermsForNeed[query.Phrase]);

            outcomes.Add(new NeedSearchOutcome(
                query.Phrase,
                found.Count,
                found.ToList().FindIndex(product => product.Sku == query.ExpectedSku) + 1));
        }

        return outcomes;
    }

    private static ProductCatalogService TheCatalogue()
        => new(RepoRoot.Combine("src", "shared", "data", "products.json"), null);

    private static IEnumerable<int> PositionsAt(IReadOnlyList<MeasuredWeight> measured, double weight)
        => measured.Single(outcome => outcome.NameFieldScoreBoost == weight).Outcomes.Select(outcome => outcome.Position);

    /// <summary>The measurement, written down where a person changing the scoring will see it.</summary>
    private void WriteTheTable(IReadOnlyList<MeasuredWeight> measured)
    {
        foreach (var weight in measured)
        {
            output.WriteLine($"name-field weight {weight.NameFieldScoreBoost}:");
            output.WriteLine("  need                                            pool  position");

            foreach (var outcome in weight.Outcomes)
            {
                var position = outcome.Position == 0 ? "unreachable" : $"#{outcome.Position}";

                output.WriteLine($"  {outcome.Need,-46} {outcome.PoolSize,4}  {position}");
            }

            var reachable = weight.Outcomes.Where(outcome => outcome.Position > 0).ToList();

            output.WriteLine(
                $"  reachable {reachable.Count} of {weight.Outcomes.Count}, "
                + $"mean position {(reachable.Count == 0 ? 0 : reachable.Average(outcome => outcome.Position)):F2}, "
                + $"worst {reachable.Max(outcome => outcome.Position)}");
            output.WriteLine(string.Empty);
        }
    }

    /// <summary>One weight, and what every need did at it.</summary>
    private sealed record MeasuredWeight(double NameFieldScoreBoost, IReadOnlyList<NeedSearchOutcome> Outcomes);

    /// <summary>One need's outcome: who it was, how big the pool was, and where its product landed.</summary>
    private sealed record NeedSearchOutcome(string Need, int PoolSize, int Position);
}
