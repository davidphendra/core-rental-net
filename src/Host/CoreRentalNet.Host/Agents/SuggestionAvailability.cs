namespace CoreRentalNet.Host.Agents;

/// <summary>
/// Whether the AI feature may be shown at all, and why not when it may not.
/// </summary>
/// <remarks>
/// <para>
/// <b>A singleton with one mutable fact, and it is mutable because the answer arrives after the container is
/// built.</b> Two things decide it: whether the deployment has been told where the agent and the embedding
/// deployment are — configuration, known at registration — and whether the catalogue index is usable, which is
/// only knowable by reading the database. Registration happens before that read, so the read writes its answer
/// here instead of rebuilding the container.
/// </para>
/// <para>
/// <b>It starts available and can only be turned off.</b> The direction matters: a deployment that has been
/// configured and whose index happens to be unreadable hides the feature rather than showing a broken one, and
/// nothing can turn a hidden feature back on at run time. The alternative — starting hidden — would need a
/// second decision point and would fail in the direction that costs a customer a run.
/// </para>
/// <para>
/// The reason is diagnostic. It goes to the log, where an operator reads it, and never to the page, which says
/// only that the feature is not there.
/// </para>
/// </remarks>
internal sealed class SuggestionAvailability
{
    /// <summary>True until something says otherwise. Read by the section and by the agent's own registration.</summary>
    public bool IsAvailable { get; private set; } = true;

    /// <summary>The last thing that hid the feature. Empty while it is available.</summary>
    public string Reason { get; private set; } = string.Empty;

    /// <summary>Hides the feature, recording why. Idempotent, and it never unhides.</summary>
    public void Hide(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        IsAvailable = false;
        Reason = reason;
    }
}
