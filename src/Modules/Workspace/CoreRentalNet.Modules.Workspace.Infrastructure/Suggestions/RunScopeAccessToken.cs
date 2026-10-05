namespace CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;

/// <summary>Carries one run's context from the endpoint into the invocation pipeline.</summary>
/// <remarks>
/// <b>The agent is built once and shared, and the run is not,</b> so the context cannot be a property of the
/// agent. An <c>AsyncLocal</c> keeps two concurrent runs from presenting each other's token. The endpoint saves
/// and restores the previous value around the run — the scope is the endpoint's rather than this holder's,
/// because the run's enumeration cannot sit inside a scope and the endpoint already brackets it.
/// </remarks>
public static class RunScopeAccessToken
{
    private static readonly AsyncLocal<RunContext?> RunContextInProgress = new();

    /// <summary>The context of the run in progress, or null when no run is being started.</summary>
    public static RunContext? Current
    {
        get => RunContextInProgress.Value;
        set => RunContextInProgress.Value = value;
    }
}
