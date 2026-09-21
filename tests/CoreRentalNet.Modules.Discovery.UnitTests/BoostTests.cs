using AwesomeAssertions;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Discovery.Application.Shortlist;
using Xunit;

namespace CoreRentalNet.Modules.Discovery.UnitTests;

/// <summary>
/// What a customer's past choices are allowed to do to a shortlist.
/// </summary>
/// <remarks>
/// The rule has two halves and the second is the one that matters: the signal may REORDER a bucket and may never
/// change who is in it. The whole feedback loop — not shown, therefore never chosen, therefore a worse score,
/// therefore not shown again — lives in the half that is refused here.
/// </remarks>
public sealed class BoostTests
{
    private static float[] Query => [1f, 0f];

    private static readonly CatalogBucket Bucket = new(CatalogCategory.Desk, null);

    /// <summary>Three products in one bucket at 0.9, 0.8 and 0.7 similarity.</summary>
    private static CatalogCandidate[] Bucket3() =>
    [
        new("BEST", Bucket, [0.9f, MathF.Sqrt(1 - (0.9f * 0.9f))]),
        new("MIDDLE", Bucket, [0.8f, MathF.Sqrt(1 - (0.8f * 0.8f))]),
        new("WORST", Bucket, [0.7f, MathF.Sqrt(1 - (0.7f * 0.7f))]),
    ];

    [Fact] // SCR-27
    public void A_product_customers_take_moves_up_within_its_bucket()
    {
        // Taken every time it was offered, against two products that have never been chosen. The weight is large
        // enough to lift the least similar product over the most similar one, which is what makes the reorder
        // unambiguous rather than a tie broken by name.
        var boost = new SelectionBoost(0.5, new Dictionary<string, double> { ["WORST"] = 1d });

        var shortlist = CatalogRanking.TopPerBucket(Query, Bucket3(), 3, boost);

        shortlist.Select(item => item.Sku).Should().Equal("WORST", "BEST", "MIDDLE");
    }

    [Fact] // SCR-27
    public void The_boost_can_reorder_but_never_change_who_is_in_the_shortlist()
    {
        // The property the whole design rests on, and the first attempt at this test proved it by accident: a
        // boost strong enough to lift the WORST product did NOTHING, because WORST is not among the two nearest
        // and the boost cannot reach outside them. That is the refusal working. So the product being boosted here
        // is one that similarity already chose - MIDDLE - and the set stays the same while the order changes.
        var boost = new SelectionBoost(5, new Dictionary<string, double> { ["MIDDLE"] = 1d });

        var plain = CatalogRanking.TopPerBucket(Query, Bucket3(), 2).Select(item => item.Sku).ToArray();
        var boosted = CatalogRanking.TopPerBucket(Query, Bucket3(), 2, boost).Select(item => item.Sku).ToArray();

        boosted.Should().BeEquivalentTo(plain);
        boosted.Should().NotEqual(plain, "an ORDER that never changed would mean the boost does nothing");
    }

    [Fact] // SCR-28
    public void A_product_that_was_never_offered_scores_no_boost_and_divides_by_nothing()
    {
        // No row means no ratio to express, not a division by zero: a product nobody has seen is not a product
        // customers dislike, and punishing it for being new is how a ranking stops being able to discover.
        var boost = new SelectionBoost(0.5, new Dictionary<string, double>());

        boost.RatioOf("NEVER-SEEN").Should().Be(0);
        boost.Score(0.8, "NEVER-SEEN").Should().Be(0.8);
    }

    [Fact] // SCR-28
    public void The_boost_is_additive_and_bounded_by_its_weight()
    {
        // Never subtractive: a product that has never been chosen keeps exactly its similarity. And the most a
        // perfect record can add is the weight itself, so a well-liked product can never leapfrog an arbitrarily
        // better match.
        var boost = new SelectionBoost(0.25, new Dictionary<string, double> { ["TAKEN"] = 1d });

        boost.Score(0.5, "IGNORED").Should().Be(0.5);
        boost.Score(0.5, "TAKEN").Should().Be(0.75);
    }

    [Fact] // SCR-28
    public void A_weight_of_nothing_leaves_the_order_alone()
    {
        // The signal switched off is the behaviour the epic shipped before this story: pure similarity.
        var off = new SelectionBoost(0, new Dictionary<string, double> { ["WORST"] = 1d });

        CatalogRanking.TopPerBucket(Query, Bucket3(), 3, off).Select(item => item.Sku)
            .Should().Equal(CatalogRanking.TopPerBucket(Query, Bucket3(), 3).Select(item => item.Sku));
    }

    [Fact] // SCR-28
    public void A_product_boosted_in_one_bucket_does_not_move_in_another()
    {
        // The score is per product, and a product belongs to exactly one bucket - so a signal about one product
        // cannot silently reorder a bucket it is not in.
        var chairs = new CatalogBucket(CatalogCategory.Chair, null);
        var candidates = new[]
        {
            new CatalogCandidate("DESK", Bucket, [1f, 0f]),
            new CatalogCandidate("CHAIR", chairs, [1f, 0f]),
        };

        var shortlist = CatalogRanking.TopPerBucket(
            Query,
            candidates,
            1,
            new SelectionBoost(0.1, new Dictionary<string, double> { ["DESK"] = 1d }));

        shortlist.Should().HaveCount(2);
        shortlist.Select(item => item.Bucket).Should().BeEquivalentTo([Bucket, chairs]);
    }
}
