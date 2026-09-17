namespace CoreRentalNet.Host.Presentation;

/// <summary>
/// Keeps one run in flight per customer, and a short gap between runs.
/// </summary>
/// <remarks>
/// A cost control rather than a rate limiter: one submitted request costs up to ten model calls and an
/// external round trip, and the realistic way that multiplies is not malice but a double click, a slow
/// page and an impatient second Enter. It limits how often a run <em>starts</em>, never how long one runs.
/// </remarks>
public interface IAiRunGuard
{
    /// <summary>
    /// Takes the run slot for a customer, or refuses because they already have one.
    /// </summary>
    /// <returns><c>false</c> when a run is in flight or the last one started too recently.</returns>
    bool TryBegin(string principal);

    /// <summary>
    /// Gives the slot back.
    /// </summary>
    /// <remarks>
    /// Called on every terminal path including cancellation, because a customer who stopped a run must not
    /// be punished for it by being unable to start another.
    /// </remarks>
    void Release(string principal);
}
