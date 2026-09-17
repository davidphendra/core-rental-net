namespace CoreRentalNet.Host.Presentation;

/// <summary>
/// One run at a time per customer, held in the process.
/// </summary>
/// <remarks>
/// <para>
/// Process-local, and that is correct here rather than a shortcut: the application is documented never to
/// scale out, so there is no second process for this state to disagree with. A distributed guard would
/// buy nothing and cost a network round trip on the path it is meant to make cheaper.
/// </para>
/// <para>
/// Keyed on the principal, so one customer's run never blocks another's, and it is the customer rather
/// than the browser because the cost being controlled is the account's.
/// </para>
/// </remarks>
internal sealed class AiRunGuard(AiRunSettings settings) : IAiRunGuard
{
    private readonly Lock gate = new();
    private readonly Dictionary<string, (DateTimeOffset Started, DateTimeOffset? Finished)> runs =
        new(StringComparer.Ordinal);

    public bool TryBegin(string principal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(principal);

        var now = DateTimeOffset.UtcNow;

        lock (gate)
        {
            if (runs.TryGetValue(principal, out var last))
            {
                // In flight, or started so recently that this is a second click rather than a second run.
                if (last.Finished is null || now - last.Started < settings.Cooldown)
                {
                    return false;
                }
            }

            runs[principal] = (now, null);

            return true;
        }
    }

    public void Release(string principal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(principal);

        lock (gate)
        {
            // Kept rather than removed, because the cooldown is measured from when the run started and
            // forgetting the entry would let a customer start again the moment the first one ended.
            if (runs.TryGetValue(principal, out var last) && last.Finished is null)
            {
                runs[principal] = (last.Started, DateTimeOffset.UtcNow);
            }
        }
    }
}
