using AwesomeAssertions;
using CoreRentalNet.Modules.Discovery.Application.Selection;
using Xunit;

namespace CoreRentalNet.Modules.Discovery.UnitTests;

/// <summary>
/// How much of a selection survives time.
/// </summary>
/// <remarks>
/// <para>
/// It is a pure function precisely so this file can exist: the write path is proved against a real database, and
/// the arithmetic behind it is proved here, where a wrong exponent is a failing number rather than a row that
/// looks plausible.
/// </para>
/// <para>
/// <b>The property that matters most is the last one.</b> The score is a RATIO of two counters, and the reason
/// the read path needs no clock at all is that uniform decay cancels out of a ratio. If that ever stopped being
/// true, retrieval would have to start decaying on read — and would then depend on when it happened to run.
/// </para>
/// </remarks>
public sealed class DecayTests
{
    private static readonly TimeSpan HalfLife = TimeSpan.FromDays(30);

    [Fact] // SCR-25
    public void A_half_life_leaves_half()
    {
        SelectionDecay.Multiplier(TimeSpan.FromDays(30), HalfLife).Should().BeApproximately(0.5, 0.0000001);
    }

    [Fact] // SCR-25
    public void Two_half_lives_leave_a_quarter()
    {
        SelectionDecay.Multiplier(TimeSpan.FromDays(60), HalfLife).Should().BeApproximately(0.25, 0.0000001);
    }

    [Fact] // SCR-25
    public void Nothing_elapsed_leaves_all_of_it()
    {
        SelectionDecay.Multiplier(TimeSpan.Zero, HalfLife).Should().Be(1);
    }

    [Fact] // SCR-25
    public void A_clock_that_went_backwards_cannot_make_a_selection_worth_more()
    {
        // TimeProvider permits a test to move time backwards, and a negative multiplier would turn decay into
        // growth: an old selection would become stronger than a fresh one, and nothing would report it.
        SelectionDecay.Multiplier(TimeSpan.FromDays(-10), HalfLife).Should().Be(1);
    }

    [Fact] // SCR-25
    public void More_time_never_leaves_more()
    {
        var previous = double.MaxValue;

        foreach (var days in new[] { 0, 1, 7, 30, 90, 365, 3650 })
        {
            var multiplier = SelectionDecay.Multiplier(TimeSpan.FromDays(days), HalfLife);

            multiplier.Should().BeLessThanOrEqualTo(previous, "decay can only ever reduce what a past choice is worth");
            previous = multiplier;
        }
    }

    [Fact] // SCR-25
    public void A_half_life_that_is_not_a_positive_span_is_refused()
    {
        // A zero half-life would divide by zero and produce an infinity or a NaN, and NaN compares false against
        // everything - so every ordering decision involving it would become unspecified.
        var act = () => SelectionDecay.Multiplier(TimeSpan.FromDays(1), TimeSpan.Zero);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact] // SCR-26
    public void The_ratio_survives_uniform_decay_which_is_why_the_read_path_needs_no_clock()
    {
        // Offered twelve times, chosen three: a quarter. Whatever the elapsed time, both counters are multiplied
        // by the same factor, so the ratio is unchanged - and the stored value therefore means the same thing
        // read today as it did the moment it was written.
        const double chosen = 3;
        const double offered = 12;

        foreach (var days in new[] { 0, 1, 30, 90, 3650 })
        {
            var multiplier = SelectionDecay.Multiplier(TimeSpan.FromDays(days), HalfLife);
            var decayed = (chosen * multiplier) / (offered * multiplier);

            decayed.Should().BeApproximately(chosen / offered, 0.0000001);
        }
    }
}
