namespace CoreRentalNet.Modules.Discovery.Application.Shortlist;

/// <summary>
/// The shortlist could not be built because the embedding deployment did not provide a query vector.
/// </summary>
/// <remarks>
/// <para>
/// <b>Typed so that a caller can tell an outage from a defect.</b> A run given this exception reports itself
/// unavailable and the customer retries; a run given anything else has hit something in this application and
/// must not be dressed up as a transient outage. Catching every exception and calling it "unavailable" is how
/// a bug becomes a support ticket that says "try again later" for a year.
/// </para>
/// <para>
/// <b>Cancellation is deliberately not this.</b> A customer who stopped the run, or navigated away, ends the run
/// in a neutral stopped state — which the application models separately and reports differently. Wrapping it
/// here would turn a deliberate action into a failure.
/// </para>
/// </remarks>
public sealed class ShortlistUnavailableException : Exception
{
    /// <summary>The failure on its own.</summary>
    public ShortlistUnavailableException(string message)
        : base(message)
    {
    }

    /// <summary>The failure, with what the deployment or transport said underneath it.</summary>
    public ShortlistUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
