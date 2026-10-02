namespace CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;

public sealed record RunContext(string AccessToken);

/// <summary>Carries one run's token from the adapter into the invocation pipeline.</summary>
/// <remarks>
/// <b>The agent is built once and shared, and the run is not,</b> so the token cannot be a property of the agent.
/// An <c>AsyncLocal</c> keeps two concurrent runs from presenting each other's token, and restoring the previous
/// value on dispose makes the scope stack-like, so nesting is safe and nothing bleeds into later work.
/// </remarks>
public static class RunScopeAccessToken
{
    private static readonly AsyncLocal<RunContext?> RunContextInProgress = new();

    /// <summary>Carries one caller's token for as long as the returned scope is alive.</summary>
    public static RunContext? Current
    {
        get => RunContextInProgress.Value;
        set => RunContextInProgress.Value = value;
    }
}
