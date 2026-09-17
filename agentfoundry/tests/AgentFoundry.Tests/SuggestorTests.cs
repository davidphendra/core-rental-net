using AgentFoundry.WorkspaceSuggestions.Catalogue;
using AgentFoundry.WorkspaceSuggestions.Contracts;
using AgentFoundry.WorkspaceSuggestions.Selection;
using AgentFoundry.WorkspaceSuggestions.Specifications;
using AgentFoundry.WorkspaceSuggestions.Vocabularies;
using AwesomeAssertions;
using Xunit;

namespace AgentFoundry.Tests;

/// <summary>
/// The candidates: one read, one per tier, and a disclosure for every slot that could not differ.
/// </summary>
/// <remarks>
/// The catalogue is a hand-written page with known prices, so the tier a product lands in is an exact
/// expectation rather than a description. That is the point of choosing price position over anything
/// cleverer: the answer is reproducible and a test can state it.
/// </remarks>
public sealed class SuggestorTests
{
    [Fact] // AGT-08
    public async Task Each_tier_takes_the_product_at_its_position_in_the_slot()
    {
        var options = await SelectAsync(Specification(Slot("Desk", 1), Slot("Monitor", 1)));

        options.Should().HaveCount(3);
        Monitors(options).Should().Equal("MON-A", "MON-C", "MON-E");
        Desks(options).Should().Equal("DSK-A", "DSK-B", "DSK-C");
    }

    [Fact] // AGT-08
    public async Task Products_at_the_same_price_are_ordered_by_sku()
    {
        // Reproducibility: two products the same price must not swap places between runs, or the same
        // request answers differently and nothing downstream can tell.
        var page = APage(samePrice: true);
        var options = (await new Suggestor(new FakeCatalogue(page))
            .SelectAsync(Specification(Slot("Monitor", 1)))).Options;

        Monitors(options).Should().Equal("MON-A", "MON-B", "MON-C");
    }

    [Fact] // AGT-09
    public async Task A_slot_with_too_few_candidates_is_pinned_in_every_tier_and_disclosed()
    {
        // One lamp in the catalogue, so the lamp slot cannot differ between the three options. The tier
        // stops meaning anything for that slot, and saying so is the honest answer.
        var options = await SelectAsync(Specification(Slot("Desk", 1), Slot("Lamp", 1)));

        options.Should().HaveCount(3);
        options.SelectMany(option => option.Lines).Where(line => line.Slot == "Lamp")
            .Select(line => line.Sku).Should().OnlyContain(sku => sku == "LMP-A");
        options.Should().OnlyContain(option => option.PinnedSlots.Contains("Lamp"));
    }

    [Fact] // AGT-10
    public async Task When_every_slot_is_pinned_one_candidate_is_sent()
    {
        // Three identical candidates would be three options that are not options, and the tier on each
        // would mean nothing. One is the honest count.
        var options = await SelectAsync(Specification(Slot("Lamp", 1)));

        options.Should().ContainSingle();
        options[0].Tier.Should().Be(Tier.Middle, "neither extreme, when there is nothing to choose between");
    }

    [Fact] // AGT-11
    public async Task A_criterion_is_matched_to_a_slot_a_quantity_a_tag_or_an_attribute()
    {
        var options = await SelectAsync(
            Specification("a standing desk with two monitors and something reliable", Slot("Desk", 1), Slot("Monitor", 2)));

        var low = options[0];

        low.Criteria.Should().Contain("slot:Desk").And.Contain("slot:Monitor");
        low.Criteria.Should().Contain("quantity:Monitor:2");
        low.Criteria.Should().Contain("tag:standing");

        // The word the catalogue cannot express is reported with the customer's own word, never dropped.
        low.Unevaluated.Should().ContainSingle().Which.Phrase.Should().Be("reliable");
        low.Unevaluated[0].Reason.Should().Be("not_in_catalogue");
    }

    [Fact] // AGT-12
    public async Task The_catalogue_is_read_once_for_the_whole_run()
    {
        var catalogue = new FakeCatalogue(APage());

        await new Suggestor(catalogue).SelectAsync(Specification(Slot("Desk", 1), Slot("Monitor", 1), Slot("Lamp", 1)));

        // One read and local grouping: the alternative is a request per slot, and the endpoint is
        // authenticated - so every extra one is a token and a round trip for data already in hand.
        catalogue.Reads.Should().Be(1);
    }

    [Fact] // AGT-13
    public async Task A_truncated_answer_is_refused_rather_than_tiered()
    {
        // Five monitors give 300/350/400 where eight give 300/400/475. A tier computed from a set that
        // is not the set looks exactly like a right answer, so it must not be computed at all.
        var catalogue = new FakeCatalogue(APage(truncated: true));

        var action = () => new Suggestor(catalogue).SelectAsync(Specification(Slot("Monitor", 1)));

        await action.Should().ThrowAsync<IncompleteCatalogueException>().WithMessage("*not the whole set*");
    }

    [Fact] // AGT-13
    public async Task A_slot_the_catalogue_cannot_fill_is_refused()
    {
        var catalogue = new FakeCatalogue(APage());

        var action = () => new Suggestor(catalogue).SelectAsync(Specification(Slot("RelaxZone", 1)));

        await action.Should().ThrowAsync<IncompleteCatalogueException>().WithMessage("*RelaxZone*");
    }

    /// <summary>The options alone: what these tests are about. The receipt is the reviewer's, and is
    /// asserted where the reviewer uses it.</summary>
    private static async Task<IReadOnlyList<SuggestionOption>> SelectAsync(Specification specification)
        => (await new Suggestor(new FakeCatalogue(APage())).SelectAsync(specification)).Options;

    private static IEnumerable<string> Monitors(IReadOnlyList<SuggestionOption> options)
        => options.Select(option => option.Lines.Single(line => line.Slot == "Monitor").Sku);

    private static IEnumerable<string> Desks(IReadOnlyList<SuggestionOption> options)
        => options.Select(option => option.Lines.Single(line => line.Slot == "Desk").Sku);

    private static SlotRequirement Slot(string slot, int quantity) => new(slot, quantity, Inferred: false);

    private static Specification Specification(params SlotRequirement[] slots)
        => Specification("a standing desk with two monitors", slots);

    private static Specification Specification(string query, params SlotRequirement[] slots)
        => new(
            new SuggestionRequest(
                "0f3c4e2a-0000-4000-8000-000000000001",
                query,
                [.. Slots.All.Select(slot => new SlotRule(slot, slot, 3, IsMandatory: false))]),
            slots);

    /// <summary>A catalogue with known prices, so the tier a product lands in is an exact expectation.</summary>
    private static CataloguePage APage(bool truncated = false, bool samePrice = false)
    {
        var monitors = samePrice
            ? new[] { Monitor("MON-A", 300), Monitor("MON-B", 300), Monitor("MON-C", 300) }
            :
            [
                Monitor("MON-A", 300), Monitor("MON-B", 325), Monitor("MON-C", 350),
                Monitor("MON-D", 375), Monitor("MON-E", 400),
            ];

        var desks = new[]
        {
            Desk("DSK-A", 600, "sit-stand", ["standing", "adjustable"]),
            Desk("DSK-B", 700, "task", ["task"]),
            Desk("DSK-C", 800, "task", ["task"]),
        };

        var lamps = new[] { Accessory("LMP-A", "lamp", 100) };

        var products = monitors.Concat(desks).Concat(lamps).ToArray();

        // The page reports what matched as well as what it carries, which is how a short answer is
        // told apart from a complete one.
        return new CataloguePage(
            truncated ? [.. products.Take(products.Length - 1)] : products,
            truncated ? products.Length - 1 : products.Length,
            products.Length,
            truncated,
            "IDR");
    }

    private static CatalogueItem Monitor(string sku, decimal price)
        => Accessory(sku, "monitor", price);

    private static CatalogueItem Accessory(string sku, string subCategory, decimal price)
        => new(sku, $"{sku} name", "accessory", subCategory, price, "A description.", new CatalogueMetadata([subCategory], new Dictionary<string, string>()));

    private static CatalogueItem Desk(string sku, decimal price, string type, IReadOnlyList<string> tags)
        => new(
            sku,
            $"{sku} name",
            "desk",
            null,
            price,
            "A description.",
            new CatalogueMetadata([.. tags, "desk"], new Dictionary<string, string> { ["type"] = type }));

    /// <summary>The read, which this tier does without a network and counts, because one is the point.</summary>
    private sealed class FakeCatalogue(CataloguePage page) : ICatalogueReader
    {
        public int Reads { get; private set; }

        public Task<CataloguePage> ReadAsync(CancellationToken cancellationToken = default)
        {
            Reads++;

            return Task.FromResult(page);
        }
    }
}
