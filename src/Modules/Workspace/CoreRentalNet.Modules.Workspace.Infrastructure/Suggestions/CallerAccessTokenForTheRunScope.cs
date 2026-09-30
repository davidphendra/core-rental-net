namespace CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;

/// <summary>Carries one run's token from the adapter into the invocation pipeline.</summary>
/// <remarks>
/// <b>The agent is built once and shared, and the run is not,</b> so the token cannot be a property of the agent.
/// An <c>AsyncLocal</c> keeps two concurrent runs from presenting each other's token, and restoring the previous
/// value on dispose makes the scope stack-like, so nesting is safe and nothing bleeds into later work.
/// </remarks>
internal sealed class CallerAccessTokenForTheRunScope : IDisposable
{
    private static readonly AsyncLocal<string?> TokenOfTheRunInProgress = new();

    private readonly string? _tokenOfTheEnclosingRun;

    private CallerAccessTokenForTheRunScope(string? callerAccessToken)
    {
        _tokenOfTheEnclosingRun = TokenOfTheRunInProgress.Value;
        TokenOfTheRunInProgress.Value = callerAccessToken;
    }

    /// <summary>The token of the run this asynchronous flow is serving, if it has one.</summary>
    public static string? TokenBeingCarried => TokenOfTheRunInProgress.Value;

    /// <summary>Carries one caller's token for as long as the returned scope is alive.</summary>
    public static CallerAccessTokenForTheRunScope CarryTheTokenOf(string? callerAccessToken)
        => new(callerAccessToken);

    public void Dispose() => TokenOfTheRunInProgress.Value = _tokenOfTheEnclosingRun;
}
