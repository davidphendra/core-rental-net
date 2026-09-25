namespace WorkspaceSuggestions.Tools;

/// <summary>The catalogue credential of the request in flight: one instance per call, and nothing else.</summary>
/// <remarks>
/// Registered scoped, so the agent that reads the token out of the request and the HTTP handler that puts it on
/// the wire are handed the same instance. Nothing is ambient, so nothing leaks between concurrent calls.
/// </remarks>
internal interface IMcpAccessTokenService
{
    /// <summary>The token this call presents, or null until the call's agent has read it from the request.</summary>
    string? Token { get; set; }

    /// <summary>The token, or a failure when the request carried none.</summary>
    ValueTask<string> GetAsync(CancellationToken cancellationToken);
}
