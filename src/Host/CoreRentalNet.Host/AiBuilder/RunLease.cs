namespace CoreRentalNet.Host.AiBuilder;

/// <summary>A customer's claim on a run, released exactly once however the run ends.</summary>
/// <remarks>
/// Disposing twice is safe and releases nothing the second time, because the endpoint releases it in a
/// <c>using</c> and a faulted stream can also release it on its own failure path. A release that could run
/// twice would let a customer's second run in while the first was still finishing.
/// </remarks>
internal sealed class RunLease(Action release) : IDisposable
{
    private Action? _release = release;

    public void Dispose() => Interlocked.Exchange(ref _release, null)?.Invoke();
}
