using AwesomeAssertions;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Discovery.Application.Shortlist;
using Xunit;

namespace CoreRentalNet.Modules.Discovery.UnitTests;

/// <summary>
/// The shape of the shortlist: two from every bucket, and no padding when a bucket is thin.
/// </summary>
/// <remarks>
/// The ranking is pure, so it is tested with candidates and a query rather than through SQLite. Every rule the
/// shortlist has is decided here — how many come from each bucket, what happens when one holds fewer, and what
/// order they arrive in — and a rule that could only be exercised through a database would be tested once,
/// coarsely, in the tier that has a file.
/// </remarks>
public sealed class ShortlistTests
{
    /// <summary>The query every candidate is measured against: [1, 0].</summary>
    private static float[] Query => [1f, 0f];

    /// <summary>The seven buckets the real catalogue fills, in the order the ranking returns them.</summary>
    private static readonly CatalogBucket[] Buckets =
    [
        new(CatalogCategory.Desk, null),
        new(CatalogCategory.Chair, null),
        new(CatalogCategory.Accessory, CatalogSubCategory.Monitor),
        new(CatalogCategory.Accessory, CatalogSubCategory.Lamp),
        new(CatalogCategory.Accessory, CatalogSubCategory.Plant),
        new(CatalogCategory.Accessory, CatalogSubCategory.Coffee),
        new(CatalogCategory.Accessory, CatalogSubCategory.Beanbag),
    ];

    /// <summary>Products for one bucket, each nearer the query than the last.</summary>
    private static IEnumerable<CatalogCandidate> In(CatalogBucket bucket, int count)
        => Enumerable.Range(0, count).Select(index => new CatalogCandidate(
            Sku: $"{bucket.Category}-{bucket.SubCategory}-{index}",
            Bucket: bucket,
            Embedding: [1f, index / 10f]));

    [Fact] // SCR-08
    public void Every_bucket_contributes_two_and_the_shortlist_holds_fourteen()
    {
        var candidates = Buckets.SelectMany(bucket => In(bucket, 5)).ToArray();

        var shortlist = CatalogRanking.TopPerBucket(Query, candidates, ShortlistSettings.DefaultPerBucket);

        shortlist.Should().HaveCount(14);
        shortlist.GroupBy(item => item.Bucket).Should().HaveCount(7);
        shortlist.GroupBy(item => item.Bucket).Should().OnlyContain(bucket => bucket.Count() == 2);
    }

    [Fact] // SCR-08
    public void A_bucket_the_catalogue_has_nothing_for_contributes_nothing_and_is_not_an_error()
    {
        // Six of the seven buckets have products. The seventh is a slot the composition will leave out, which
        // is a smaller shortlist and not a failure - padding it would offer the model products that do not
        // exist.
        var candidates = Buckets.Skip(1).SelectMany(bucket => In(bucket, 5)).ToArray();

        var shortlist = CatalogRanking.TopPerBucket(Query, candidates, ShortlistSettings.DefaultPerBucket);

        shortlist.Should().HaveCount(12);
        shortlist.Should().NotContain(item => item.Bucket == Buckets[0]);
    }

    [Fact] // SCR-08
    public void A_bucket_holding_one_product_contributes_one_rather_than_repeating_it()
    {
        // The first bucket has one product and the other six have five: twelve from the six, one from this one,
        // and no product offered twice to reach a number.
        var candidates = Buckets.Skip(1).SelectMany(bucket => In(bucket, 5)).Concat(In(Buckets[0], 1)).ToArray();

        var shortlist = CatalogRanking.TopPerBucket(Query, candidates, ShortlistSettings.DefaultPerBucket);

        shortlist.Should().HaveCount(13);
        shortlist.Where(item => item.Bucket == Buckets[0]).Should().ContainSingle();
        shortlist.Select(item => item.Sku).Should().OnlyHaveUniqueItems();
    }

    [Fact] // SCR-08
    public void An_empty_catalogue_produces_an_empty_shortlist_rather_than_a_failure()
    {
        // The index cannot be empty - the ingestion refuses an empty catalogue - so this is the shape of the
        // answer and not a state that ships. Stated because "fewer is fewer" has to include none.
        CatalogRanking.TopPerBucket(Query, [], ShortlistSettings.DefaultPerBucket).Should().BeEmpty();
    }

    [Fact] // SCR-09
    public void The_nearest_products_come_first_within_a_bucket()
    {
        // Three candidates in one bucket, at known angles to the query [1, 0]: identical, 0.6, and orthogonal.
        // Only the nearest two are taken, and they come back nearest first - which is the order the payload
        // carries and therefore the order the model reads.
        var bucket = Buckets[0];
        var candidates = new[]
        {
            new CatalogCandidate("ORTHOGONAL", bucket, [0f, 1f]),
            new CatalogCandidate("NEAR", bucket, [1f, 0f]),
            new CatalogCandidate("MIDDLE", bucket, [3f, 4f]),
        };

        var shortlist = CatalogRanking.TopPerBucket(Query, candidates, ShortlistSettings.DefaultPerBucket);

        shortlist.Select(item => item.Sku).Should().Equal("NEAR", "MIDDLE");
        shortlist.Select(item => item.Score).Should().BeInDescendingOrder();
    }

    [Fact] // SCR-13
    public void No_product_is_offered_twice()
    {
        // There is no de-duplication line in the ranking, and this is why: the vector table is keyed by SKU, so
        // one product is one row, and a product's bucket comes from the catalogue's total mapping, so one
        // product is in one bucket. A duplicate would need either a second row for a SKU or a SKU in two
        // buckets, and neither can be produced. The rule is asserted rather than coded, and the strong version
        // of it runs against the real 205 in CatalogShortlistTests.
        var candidates = Buckets.SelectMany(bucket => In(bucket, 5)).ToArray();

        var shortlist = CatalogRanking.TopPerBucket(Query, candidates, ShortlistSettings.DefaultPerBucket);

        shortlist.Select(item => item.Sku).Should().OnlyHaveUniqueItems();
    }

    [Fact] // SCR-08
    public void A_vector_of_another_width_is_refused_rather_than_compared()
    {
        // Comparing two widths is meaningless, and the failure has to be loud: the freshness check refuses an
        // index of the wrong width at start-up, so reaching here means something is wrong that silence would
        // hide behind fourteen plausible-looking products.
        var candidates = new[] { new CatalogCandidate("SKU1", Buckets[0], [1f, 0f, 0f]) };

        var act = () => CatalogRanking.TopPerBucket(Query, candidates, ShortlistSettings.DefaultPerBucket);

        act.Should().Throw<InvalidOperationException>().WithMessage("*two widths*");
    }
}
