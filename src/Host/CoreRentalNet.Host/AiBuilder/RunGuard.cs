using System.Collections.Concurrent;
using System.Security.Claims;

namespace CoreRentalNet.Host.AiBuilder;

/// <summary>One run in flight per customer, and no numeric cap.</summary>
/// <remarks>
/// <para>
/// A run is paid and abusable, so a customer may have exactly one in flight. The lease it hands out is
/// <see cref="IDisposable"/> on purpose: <c>using</c> releases it on every exit path there is — a return, a
/// throw, a cancelled token — which is what stops a stopped run wedging its customer out of the feature
/// until the process restarts.
/// </para>
/// <para>
/// <b>No numeric per-customer cap is set.</b> A placeholder would look tuned without being so; the run
/// record carries tokens, model and model-call count so the evaluation tier can set the cap from
/// measurement, and this guard deliberately counts nothing.
/// </para>
/// <para>
/// In-process, therefore per-instance: two application instances would each allow one run. That is the same
/// shape as the rest of this application's in-memory state and is recorded rather than hidden — a
/// deployment that scales out needs this in a shared store.
/// </para>
/// </remarks>
internal sealed class RunGuard
{
    private readonly ConcurrentDictionary<string, byte> _running = new(StringComparer.Ordinal);

    /// <summary>Begins a run, or returns null when this customer already has one in flight.</summary>
    public RunLease? TryBegin(ClaimsPrincipal customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        var key = CustomerKey.Of(customer);

        // An account with nothing stable to key on cannot be held to one run, so it is refused rather
        // than allowed to run unlimited times under a shared key.
        if (key is null || !_running.TryAdd(key, 0))
        {
            return null;
        }

        return new RunLease(() => _running.TryRemove(key, out _));
    }
}
