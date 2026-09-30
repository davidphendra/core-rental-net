namespace CoreRentalNet.Agents.Shared.Model;

/// <summary>How the model transport behaves when a call is slow or fails, stated rather than left to a default.</summary>
/// <remarks>
/// <para>
/// <b>These are transport retries, not attempts.</b> A run's business attempt count is the workflow's, and it is
/// what bounds how many times the sentence is read. This number bounds how many times one model call is retried
/// when the transport fails, and the two must never share a counter: a run that has made three attempts has not
/// made three of these, and a call retried twice has not used two of those.
/// </para>
/// <para>
/// The run carries a large context, so a call can legitimately take longer than the hundred-second client
/// default. The client does not surface a slow call as slow - it cancels and retries - so the budget is stated
/// here.
/// </para>
/// </remarks>
public sealed record ModelTransportRetryOptions
{
    /// <summary>How long one model call may take before the transport gives up on it.</summary>
    public TimeSpan NetworkTimeout { get; init; } = TimeSpan.FromMinutes(10);

    /// <summary>How many times one failed model call is retried by the transport.</summary>
    public int MaximumRetryAttempts { get; init; } = 2;
}
