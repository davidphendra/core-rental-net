namespace CoreRentalNet.Agents.Shared.Mcp;

internal interface IAccessTokenHeaderReader
{
    /// <summary>The token this invocation carries, or null when it carried none.</summary>
    string? ReadCallerAccessToken();
}

/// <summary>Reads the caller's catalogue token off the invocation the run arrived in.</summary>
/// <remarks>
/// <para>
/// <b>The token is out of band from the request body.</b> The body is what the hosting layer records and what a
/// model may be handed, and a bearer token belongs in neither — so this is how the run is given one without any
/// stage having to take it back out of a message.
/// </para>
/// <para>
/// <b>Only the presence is ever logged, never the value.</b> A token in a log line is a token on its way out of
/// the trust boundary, and log lines travel further than request bodies do.
/// </para>
/// </remarks>
internal sealed class AccessTokenHeaderReader(
    IHttpContextAccessor httpContextAccessor,
    ILogger<AccessTokenHeaderReader> logger) : IAccessTokenHeaderReader
{
    /// <summary>The token this invocation carries, or null when it carried none.</summary>
    public string? ReadCallerAccessToken()
    {
        var callerAccessToken = string.Empty;

        if (httpContextAccessor.HttpContext?.Request.Headers.ContainsKey(AccessTokenHeader.Name) ?? false)
        {
            var carriedValue = httpContextAccessor.HttpContext?.Request.Headers[AccessTokenHeader.Name];
            callerAccessToken = carriedValue is { Count: > 0 } ? carriedValue.ToString() : null;
        }

        logger.LogInformation(
            "Caller catalogue token {Presence} on the invocation.",
            string.IsNullOrEmpty(callerAccessToken) ? "absent" : "present");

        return callerAccessToken;
    }
}
