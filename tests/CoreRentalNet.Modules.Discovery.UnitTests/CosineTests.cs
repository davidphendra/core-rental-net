using AwesomeAssertions;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Discovery.Application.Shortlist;
using Xunit;

namespace CoreRentalNet.Modules.Discovery.UnitTests;

/// <summary>
/// The similarity the ranking is built on.
/// </summary>
/// <remarks>
/// <para>
/// It is asserted through <see cref="CatalogRanking.TopPerBucket"/> rather than by reaching for a private
/// method, because the score that comes back is the score a caller sees: the ranking publishes it on every
/// item, so a test that checks it is checking the contract rather than an implementation detail.
/// </para>
/// <para>
/// <b>There is one implementation and no faster one, which is worth stating because the task that asked for
/// these tests expected two.</b> The plan said to prove an optimised form agreed with a naive one; no
/// optimisation was written, because the arithmetic is a single pass over 512 floats and there is nothing to
/// trust. What the tests therefore pin is the <b>mathematics</b> — the values a cosine must produce on
/// vectors whose answer can be worked out by hand — so that if a faster form is ever added, the numbers it
/// has to reproduce are already written down.
/// </para>
/// </remarks>
public sealed class CosineTests
{
    private static readonly CatalogBucket Bucket = new(CatalogCategory.Desk, null);

    /// <summary>The cosine of the query [1, 0] against one candidate.</summary>
    private static float ScoreAgainst(float[] query, float[] candidate)
        => CatalogRanking.TopPerBucket(query, [new CatalogCandidate("SKU1", Bucket, candidate)], 1).Single().Score;

    [Fact] // SCR-10
    public void A_vector_identical_to_the_query_scores_one()
        => ScoreAgainst([1f, 0f], [1f, 0f]).Should().Be(1f);

    [Fact] // SCR-10
    public void A_vector_at_right_angles_to_the_query_scores_zero()
        => ScoreAgainst([1f, 0f], [0f, 1f]).Should().Be(0f);

    [Fact] // SCR-10
    public void A_vector_opposite_the_query_scores_minus_one()
        => ScoreAgainst([1f, 0f], [-1f, 0f]).Should().Be(-1f);

    [Fact] // SCR-10
    public void A_vector_is_measured_by_direction_and_not_by_length()
    {
        // The same direction scores the same however long the vector is: an embedding of a long paragraph is
        // not "more similar" than one of a short sentence simply for being a larger number.
        ScoreAgainst([1f, 0f], [1f, 0f]).Should().Be(ScoreAgainst([1f, 0f], [100f, 0f]));
        ScoreAgainst([3f, 4f], [4f, 3f]).Should().BeApproximately(0.96f, 0.000001f);
    }

    [Fact] // SCR-10
    public void A_hand_computed_angle_comes_back_exactly()
    {
        // 60 degrees: cos(60) = 0.5 exactly, and it is the kind of number a wrong implementation gets wrong by
        // a rounding error or a missing square root.
        ScoreAgainst([1f, 0f], [0.5f, 0.8660254f]).Should().BeApproximately(0.5f, 0.00001f);
    }

    [Fact] // SCR-10
    public void A_vector_of_zeros_scores_zero_rather_than_becoming_NAN()
    {
        // A zero vector has no direction, so it divides by zero. NaN would be worse than wrong: it compares
        // false against everything, so every ordering decision involving it becomes unspecified and the
        // shortlist would depend on what the sort happened to do.
        var score = ScoreAgainst([1f, 0f], [0f, 0f]);

        score.Should().Be(0f);
        float.IsNaN(score).Should().BeFalse();
    }

    [Fact] // SCR-10
    public void Scores_are_compared_across_a_whole_range_and_stay_ordered()
    {
        // The ranking sorts on this number, so the property that matters for the shortlist is that a nearer
        // direction scores higher - not the individual value. Going round the circle: the ordering must be
        // strictly decreasing as the angle opens.
        float[] query = [1f, 0f];
        var scores = new[] { 0f, 30f, 60f, 90f, 120f, 180f }
            .Select(degrees => ScoreAgainst(
                query,
                [MathF.Cos(degrees * MathF.PI / 180f), MathF.Sin(degrees * MathF.PI / 180f)]))
            .ToArray();

        scores.Should().BeInDescendingOrder();
    }
}
