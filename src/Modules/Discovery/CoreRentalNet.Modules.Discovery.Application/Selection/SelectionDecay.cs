namespace CoreRentalNet.Modules.Discovery.Application.Selection;

/// <summary>How much of a count is left after a while.</summary>
/// <remarks>
/// <para>
/// Exponential with a half-life: after exactly one half-life, half of a count remains. A pure function, so the
/// arithmetic is unit-tested rather than only exercised through a database — and so that a faster or cleverer
/// form added later has numbers to reproduce.
/// </para>
/// <para>
/// <b>The multiplier never exceeds one and is never negative</b>, so decay can only ever reduce what a past
/// selection is worth. A clock that went backwards — which <c>TimeProvider</c> permits in a test — must not
/// INCREASE a count, which is why elapsed time is clamped rather than trusted.
/// </para>
/// </remarks>
public static class SelectionDecay
{
    /// <summary>What fraction of a count survives <paramref name="elapsed"/> time.</summary>
    public static double Multiplier(TimeSpan elapsed, TimeSpan halfLife)
    {
        if (halfLife <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(halfLife), "A half-life has to be a positive span.");
        }

        return elapsed <= TimeSpan.Zero ? 1d : Math.Pow(2, -elapsed.TotalSeconds / halfLife.TotalSeconds);
    }
}
