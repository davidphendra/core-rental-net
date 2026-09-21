using CoreRentalNet.Modules.Discovery.Application.Selection;
using Microsoft.Extensions.Logging;

namespace CoreRentalNet.Host.AiBuilder;

/// <summary>
/// Records a run's shortlist as offered, at most once per run.
/// </summary>
/// <remarks>
/// <para>
/// <b>A narrative fragment or an answer is the proof the request reached the model</b>, which is what "offered"
/// means — and an <c>Unavailable</c> event is explicitly NOT, because the adapter yields that when it cannot
/// build the agent at all or when the transport refuses. A run whose embedding failed never had a shortlist; a
/// run whose agent was never reached never showed one to anybody. Counting either inflates the denominator,
/// drops every score, and reads as "customers like nothing" — a signal that looks like a finding and is a bug.
/// </para>
/// <para>
/// <b>A signal that cannot be recorded never fails a run.</b> The answer is already arriving and a customer is
/// waiting for it; losing one offer is a fact to log, not a reason to throw away a run that has been paid for.
/// </para>
/// </remarks>
internal sealed class OfferRecorder(IOfferSelection offers, ILoggerFactory loggers)
{
    private const string Category = "CoreRentalNet.Host.AiBuilder.SelectionSignal";

    private bool _recorded;

    /// <summary>Records the shortlist once, whatever else the run does.</summary>
    public async Task RecordAsync(IReadOnlyList<string> shortlist, RunBudget budget)
    {
        if (_recorded)
        {
            return;
        }

        _recorded = true;

        try
        {
            await offers.OfferAsync(shortlist, budget.Run);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            loggers.CreateLogger(Category).LogError(
                exception,
                "The shortlist could not be recorded as offered, so this run will not count towards any score.");
        }
    }
}
