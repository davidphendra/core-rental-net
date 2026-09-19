namespace WorkspaceSuggestions.Usage;

/// <summary>Which run's usage is being counted, for the one <see cref="UsageRecorder"/> that belongs to it.</summary>
/// <remarks>
/// <para>
/// The chat client is built once and shared by every request, so it cannot own a counter: one customer's
/// tokens would land in the next customer's record. The scope is ambient instead, which follows the async
/// flow of a single run, so concurrent runs count into their own record and never into each other's.
/// </para>
/// <para>
/// Scopes nest and restore, so a run inside a run - a repair loop, or a future evaluation pass - leaves the
/// outer record exactly as it found it.
/// </para>
/// </remarks>
internal sealed class RunUsageScope : IDisposable
{
    private static readonly AsyncLocal<UsageRecorder?> Ambient = new();

    private readonly UsageRecorder? _previous;

    private RunUsageScope(UsageRecorder recorder, UsageRecorder? previous)
    {
        Recorder = recorder;
        _previous = previous;
    }

    /// <summary>The recorder for the run in flight, or null when nothing is being counted.</summary>
    public static UsageRecorder? Current => Ambient.Value;

    public UsageRecorder Recorder { get; }

    public static RunUsageScope Begin()
    {
        var scope = new RunUsageScope(new UsageRecorder(), Ambient.Value);

        Ambient.Value = scope.Recorder;

        return scope;
    }

    public void Dispose() => Ambient.Value = _previous;
}
